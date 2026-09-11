#!/usr/bin/env python3
"""Browser smoke suite for the Mundialito AngularJS client.

There are no client-side unit tests, and production runs a minified bundle that a Development
run never executes, so this drives a real headless Chrome over the DevTools protocol against
whatever `./scripts/dev.sh up` (or `up --prod-bundle`) is serving.

Two things make it more than a does-the-page-load check:

* Every request to sentry.io is answered here and never sent. The events the page *would*
  have reported are the main oracle: AngularJS catches controller and digest errors in
  $exceptionHandler, so they never reach Runtime.exceptionThrown - Sentry is the only place
  they surface, which is exactly how they were found in production.
* A rule engine can hold, fail or fake any request, which turns timing bugs into
  deterministic ones: a slow /api/account/userInfo, a phone dropping the network mid-route.

Scenarios are invariants (must always pass) or bug reproductions tagged with the change that
fixes them. `--baseline <change>` runs against the code *before* that change and expects its
bug scenarios to fail - with a message matching the production symptom, so a scenario that
fails for some unrelated reason is not mistaken for a reproduction.

Run it through `./scripts/dev.sh smoke [options]`.
"""

import argparse
import base64
import datetime as dt
import fnmatch
import hashlib
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile
import threading
import time
import traceback
import urllib.error
import urllib.parse
import urllib.request
import uuid

try:
    import websocket  # websocket-client
except ImportError:
    sys.exit('smoke.py needs websocket-client: python3 -m pip install websocket-client')

ROOT = pathlib.Path(__file__).resolve().parent.parent
APP_DIR = ROOT / 'Mundialito'
RUN_DIR = ROOT / '.dev'
ARTIFACTS = RUN_DIR / 'smoke'
CHROME = os.environ.get('CHROME', '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome')

ADMIN, PLAYER, DISABLED = 'roez', 'dev_active', 'dev_disabled'
PASSWORD = '123456'

# The changes whose bugs this suite reproduces, in the order they land. `--baseline <change>`
# means "the code before <change>": its bug scenarios, and those of every later change, are
# expected to fail.
FIXES = ['reporting', 'bet-forms', 'user-guard', 'sdk-v10']

INJ = 'angular.element(document.documentElement).injector()'

# Toasts time out after 2.5s (Index.cshtml), so record them as they appear rather than look
# for them afterwards. Installed before any page script runs, on every navigation.
TOAST_RECORDER = r"""
(function () {
  window.__smokeToasts = [];
  function scan(node) {
    if (node.nodeType !== 1) return;
    var found = node.classList.contains('toast') ? [node] : node.querySelectorAll('.toast');
    Array.prototype.forEach.call(found, function (el) {
      setTimeout(function () {
        var title = el.querySelector('.toast-title'), body = el.querySelector('.toast-message');
        window.__smokeToasts.push({
          cls: el.className,
          title: title ? title.textContent.trim() : '',
          body: body ? body.textContent.trim() : ''
        });
      }, 50);
    });
  }
  new MutationObserver(function (records) {
    records.forEach(function (r) { Array.prototype.forEach.call(r.addedNodes, scan); });
  }).observe(document, { childList: true, subtree: true });
})();
"""

# Settled means: no $http in flight, the loading overlay and route message cleared, the
# current route resolved, then Angular's own testability hook and a beat for animations.
# whenStable is raced against a timer - a long $timeout anywhere would otherwise hang it.
WAIT_STABLE = r"""
new Promise(function (resolve) {
  var t0 = Date.now(), done = false;
  function finish(state) { if (!done) { done = true; resolve(state); } }
  (function poll() {
    try {
      var inj = %(inj)s;
      if (inj) {
        var app = inj.get('$rootScope').mundialitoApp || {};
        var route = inj.get('$route').current;
        if (inj.get('$http').pendingRequests.length === 0 && !app.loading && app.message == null
            && route && route.locals) {
          angular.getTestability(document.documentElement).whenStable(function () {
            setTimeout(function () { finish('stable'); }, 300);
          });
          setTimeout(function () { finish('stable'); }, 2500);
          return;
        }
      }
    } catch (e) {}
    if (Date.now() - t0 > %(timeout)d) { finish('timeout'); return; }
    setTimeout(poll, 100);
  })();
})
"""

PAGE_STATE = r"""
(function () {
  var inj = %(inj)s;
  if (!inj) return { booted: false };
  var current = inj.get('$route').current || {};
  var app = inj.get('$rootScope').mundialitoApp || {};
  var views = document.querySelectorAll('[ng-view]');
  var text = 0;
  Array.prototype.forEach.call(views, function (v) { text = Math.max(text, v.innerText.trim().length); });
  var security = inj.get('security');
  return {
    booted: true,
    path: inj.get('$location').path(),
    route: current.originalPath || (current.$$route || {}).originalPath || null,
    viewText: text,
    hidden: !!(app.loading || app.authenticating),
    user: security.user ? security.user.Username : null
  };
})()
"""


class Failure(AssertionError):
    """A scenario's expectation did not hold. Anything else raised is a harness error."""


def check(condition, message):
    if not condition:
        raise Failure(message)


class CDPError(Exception):
    pass


class SentryEvent:
    def __init__(self, raw):
        self.raw = raw
        fp = raw.get('fingerprint')
        self.fingerprint = tuple(fp) if fp else None
        self.level = raw.get('level') or 'error'
        values = (raw.get('exception') or {}).get('values') or []
        self.exceptions = [(v.get('type') or '', v.get('value') or '') for v in values]
        message = raw.get('message')
        if isinstance(message, dict):
            message = message.get('formatted') or message.get('message')
        if not message and isinstance(raw.get('logentry'), dict):
            message = raw['logentry'].get('formatted') or raw['logentry'].get('message')
        self.message = message or ''
        tags = raw.get('tags') or {}
        self.tags = dict(tags) if isinstance(tags, list) else tags
        self.contexts = raw.get('contexts') or {}
        self.extra = raw.get('extra') or {}

    @property
    def title(self):
        if self.exceptions:
            kind, value = self.exceptions[0]
            return '%s: %s' % (kind, value) if kind else value
        return self.message

    def describe(self):
        return '%s [%s] fingerprint=%s' % (self.title[:140], self.level, list(self.fingerprint or []))


class Browser:
    """Headless Chrome plus the Sentry sink and the request rule engine.

    CDP messages are read on a background thread. Fetch.requestPaused is answered there too
    (anything that must wait - a hold, reading a large body - goes to a timer or a worker), so
    the main thread can block in Runtime.evaluate while a request it caused is being held."""

    def __init__(self, headful=False):
        RUN_DIR.mkdir(exist_ok=True)
        self.profile = tempfile.mkdtemp(prefix='smoke-profile-', dir=str(RUN_DIR))
        args = [CHROME, '--user-data-dir=' + self.profile, '--remote-debugging-port=0',
                '--remote-allow-origins=*', '--no-first-run', '--no-default-browser-check',
                '--disable-extensions', '--disable-background-networking', '--disable-sync',
                '--disable-component-update', '--disable-features=Translate',
                # Belt and braces: the Fetch sink answers every sentry.io request before DNS
                # is consulted, and anything that slipped past it would fail to resolve.
                '--host-resolver-rules=MAP *.sentry.io ~NOTFOUND',
                '--window-size=1280,900', 'about:blank']
        if not headful:
            args.insert(1, '--headless=new')
        self.proc = subprocess.Popen(args, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        port = self._devtools_port()
        targets = json.load(urllib.request.urlopen('http://127.0.0.1:%d/json/list' % port, timeout=10))
        page = next(t for t in targets if t.get('type') == 'page')
        self.ws = websocket.create_connection(page['webSocketDebuggerUrl'], timeout=None)

        self._lock = threading.Lock()
        self._next_id = 0
        self._pending = {}
        self._requests = {}
        self._loaded = threading.Event()
        self.rules = []
        self.events, self.console, self.exceptions = [], [], []
        self.responses, self.failures, self.sink_errors = [], [], []

        self._reader = threading.Thread(target=self._read, daemon=True)
        self._reader.start()
        for domain in ('Page', 'Runtime', 'Network', 'Log'):
            self.send(domain + '.enable')
        self.send('Network.setCacheDisabled', {'cacheDisabled': True})
        self.send('Page.addScriptToEvaluateOnNewDocument', {'source': TOAST_RECORDER})
        self.desktop()
        self.set_rules([])

    def _devtools_port(self):
        path = os.path.join(self.profile, 'DevToolsActivePort')
        for _ in range(150):
            if self.proc.poll() is not None:
                raise RuntimeError('Chrome exited during startup (%s)' % CHROME)
            try:
                with open(path) as f:
                    first = f.readline().strip()
                if first:
                    return int(first)
            except (OSError, ValueError):
                pass
            time.sleep(0.1)
        raise RuntimeError('Chrome did not open a DevTools port')

    # ---------------------------------------------------------------- protocol plumbing

    def _read(self):
        while True:
            try:
                raw = self.ws.recv()
            except Exception:
                return
            if not raw:
                continue
            msg = json.loads(raw)
            if 'id' in msg:
                with self._lock:
                    waiter = self._pending.pop(msg['id'], None)
                if waiter is not None:
                    waiter['msg'] = msg
                    waiter['event'].set()
            else:
                try:
                    self._dispatch(msg.get('method'), msg.get('params') or {})
                except Exception as e:  # a handler bug must not kill the reader
                    self.sink_errors.append('%s: %r' % (msg.get('method'), e))

    def send(self, method, params=None, timeout=45):
        waiter = {'event': threading.Event()}
        with self._lock:
            self._next_id += 1
            mid = self._next_id
            self._pending[mid] = waiter
            self.ws.send(json.dumps({'id': mid, 'method': method, 'params': params or {}}))
        if not waiter['event'].wait(timeout):
            raise CDPError('%s timed out' % method)
        msg = waiter['msg']
        if 'error' in msg:
            raise CDPError('%s: %s' % (method, msg['error'].get('message')))
        return msg.get('result') or {}

    def send_nowait(self, method, params=None):
        with self._lock:
            self._next_id += 1
            self.ws.send(json.dumps({'id': self._next_id, 'method': method, 'params': params or {}}))

    def _dispatch(self, method, params):
        if method == 'Fetch.requestPaused':
            self._paused(params)
        elif method == 'Runtime.consoleAPICalled':
            if params.get('type') in ('error', 'assert'):
                self.console.append(' '.join(self._arg_text(a) for a in params.get('args') or []))
        elif method == 'Runtime.exceptionThrown':
            details = params.get('exceptionDetails') or {}
            self.exceptions.append((details.get('exception') or {}).get('description') or details.get('text'))
        elif method == 'Network.requestWillBeSent':
            req = params['request']
            self._requests[params['requestId']] = (req['method'], req['url'])
        elif method == 'Network.responseReceived':
            req_method, _ = self._requests.get(params['requestId'], ('?', None))
            self.responses.append((req_method, params['response']['url'], params['response']['status']))
        elif method == 'Network.loadingFailed':
            req_method, url = self._requests.get(params['requestId'], ('?', '?'))
            self.failures.append((req_method, url, params.get('errorText'), params.get('canceled')))
        elif method == 'Page.loadEventFired':
            self._loaded.set()

    @staticmethod
    def _arg_text(arg):
        if 'value' in arg:
            return arg['value'] if isinstance(arg['value'], str) else json.dumps(arg['value'])
        return arg.get('description') or arg.get('type', '')

    # ---------------------------------------------------------------- Sentry sink + rules

    def set_rules(self, rules):
        """Replace the request rules. Each: pattern (CDP wildcard on the full URL), optional
        method, action hold|fail|fulfill, times (None = unlimited)."""
        self.rules = [dict(r, hits=0) for r in rules]
        patterns = [{'urlPattern': '*sentry.io*', 'requestStage': 'Request'}]
        patterns += [{'urlPattern': r['pattern'], 'requestStage': 'Request'} for r in self.rules]
        self.send('Fetch.enable', {'patterns': patterns})

    def _paused(self, params):
        req, rid = params['request'], params['requestId']
        if 'sentry.io' in urllib.parse.urlsplit(req['url']).netloc:
            threading.Thread(target=self._sentry, args=(params,), daemon=True).start()
            return
        for rule in self.rules:
            if rule.get('method') and rule['method'] != req['method']:
                continue
            if not fnmatch.fnmatchcase(req['url'], rule['pattern']):
                continue
            if rule.get('times') is not None and rule['hits'] >= rule['times']:
                continue
            rule['hits'] += 1
            action = rule['action']
            if action == 'hold':
                threading.Timer(rule.get('seconds', 2.0), self.send_nowait,
                                args=('Fetch.continueRequest', {'requestId': rid})).start()
            elif action == 'fail':
                self.send_nowait('Fetch.failRequest', {'requestId': rid, 'errorReason': rule.get('reason', 'Failed')})
            elif action == 'fulfill':
                body = rule.get('body', '')
                self.send_nowait('Fetch.fulfillRequest', {
                    'requestId': rid, 'responseCode': rule.get('status', 200),
                    'responseHeaders': [{'name': 'Content-Type', 'value': rule.get('content_type', 'application/json; charset=utf-8')}],
                    'body': base64.b64encode(body.encode()).decode()})
            else:
                self.send_nowait('Fetch.continueRequest', {'requestId': rid})
            return
        self.send_nowait('Fetch.continueRequest', {'requestId': rid})

    def _sentry(self, params):
        req, rid = params['request'], params['requestId']
        cors = [{'name': 'Access-Control-Allow-Origin', 'value': '*'},
                {'name': 'Access-Control-Allow-Headers', 'value': '*'},
                {'name': 'Access-Control-Allow-Methods', 'value': 'POST, OPTIONS'}]
        if req['method'] == 'OPTIONS':
            self.send_nowait('Fetch.fulfillRequest', {'requestId': rid, 'responseCode': 204, 'responseHeaders': cors})
            return
        try:
            body = req.get('postData')
            if body is None and req.get('postDataEntries'):
                body = b''.join(base64.b64decode(e.get('bytes', '')) for e in req['postDataEntries']).decode('utf-8', 'replace')
            if body is None and req.get('hasPostData') and params.get('networkId'):
                body = self.send('Network.getRequestPostData', {'requestId': params['networkId']}).get('postData')
            self._record(req['url'], body or '')
        except Exception as e:
            self.sink_errors.append('%s: %r' % (req['url'], e))
        finally:
            self.send_nowait('Fetch.fulfillRequest', {
                'requestId': rid, 'responseCode': 200,
                'responseHeaders': cors + [{'name': 'Content-Type', 'value': 'application/json'}],
                'body': base64.b64encode(json.dumps({'id': uuid.uuid4().hex}).encode()).decode()})

    def _record(self, url, body):
        """v6 posts events to /store/ as plain JSON; v7+ posts envelopes to /envelope/. Only
        event items count - sessions (sent on every page load) and client reports do not."""
        path = urllib.parse.urlsplit(url).path
        if path.endswith('/store/'):
            self.events.append(SentryEvent(json.loads(body)))
        elif path.endswith('/envelope/'):
            lines = body.split('\n')
            i = 1
            while i < len(lines):
                if not lines[i].strip():
                    i += 1
                    continue
                header = json.loads(lines[i])
                payload = lines[i + 1] if i + 1 < len(lines) else ''
                i += 2
                if header.get('type') == 'event':
                    self.events.append(SentryEvent(json.loads(payload)))

    # ---------------------------------------------------------------- page control

    def eval(self, expression, timeout=45):
        result = self.send('Runtime.evaluate', {'expression': expression, 'awaitPromise': True,
                                                'returnByValue': True}, timeout=timeout)
        if 'exceptionDetails' in result:
            details = result['exceptionDetails']
            raise CDPError('page script threw: %s' % ((details.get('exception') or {}).get('description') or details.get('text')))
        return (result.get('result') or {}).get('value')

    def navigate(self, url):
        self._loaded.clear()
        self.send('Page.navigate', {'url': url})
        self._loaded.wait(20)

    def desktop(self):
        self.send('Emulation.setDeviceMetricsOverride', {'width': 1280, 'height': 900, 'deviceScaleFactor': 1, 'mobile': False})
        self.send('Emulation.setTouchEmulationEnabled', {'enabled': False})

    def mobile(self):
        self.send('Emulation.setDeviceMetricsOverride', {'width': 375, 'height': 812, 'deviceScaleFactor': 2, 'mobile': True})
        self.send('Emulation.setTouchEmulationEnabled', {'enabled': True, 'maxTouchPoints': 5})

    def click_at(self, x, y):
        """A real press and release. ui-grid sorts on mousedown/mouseup, not on click, so
        el.click() from script would prove nothing."""
        for kind in ('mouseMoved', 'mousePressed', 'mouseReleased'):
            self.send('Input.dispatchMouseEvent', {'type': kind, 'x': x, 'y': y, 'button': 'left', 'clickCount': 1})

    def tap_at(self, x, y):
        self.send('Input.dispatchTouchEvent', {'type': 'touchStart', 'touchPoints': [{'x': x, 'y': y}]})
        self.send('Input.dispatchTouchEvent', {'type': 'touchEnd', 'touchPoints': []})

    def screenshot(self, path):
        data = self.send('Page.captureScreenshot', {'format': 'png'}).get('data')
        if data:
            pathlib.Path(path).write_bytes(base64.b64decode(data))

    def close(self):
        try:
            self.ws.close()
        except Exception:
            pass
        self.proc.terminate()
        try:
            self.proc.wait(10)
        except subprocess.TimeoutExpired:
            self.proc.kill()
        shutil.rmtree(self.profile, ignore_errors=True)


class Fixtures:
    pass


class Suite:
    """What scenarios work with: the browser, the app's API, tokens and fixture ids."""

    def __init__(self, base, browser, expect_bundle):
        self.base = base.rstrip('/')
        self.b = browser
        self.expect_bundle = expect_bundle
        self._tokens = {}
        self.f = self._fixtures()

    # ---------------------------------------------------------------- API access

    def api(self, method, path, token=None, body=None):
        req = urllib.request.Request(self.base + path, method=method,
                                     data=json.dumps(body).encode() if body is not None else None,
                                     headers={'Content-Type': 'application/json'})
        if token:
            req.add_header('Authorization', 'Bearer ' + token)
        try:
            with urllib.request.urlopen(req, timeout=20) as r:
                status, raw = r.status, r.read()
        except urllib.error.HTTPError as e:
            status, raw = e.code, e.read()
        try:
            return status, json.loads(raw) if raw else None
        except ValueError:
            return status, raw.decode('utf-8', 'replace')

    def http(self, path):
        try:
            with urllib.request.urlopen(self.base + path, timeout=20) as r:
                return r.status, r.read().decode('utf-8', 'replace')
        except urllib.error.HTTPError as e:
            return e.code, ''

    def token(self, user):
        if user not in self._tokens:
            status, body = self.api('POST', '/api/account/login', body={'Username': user, 'Password': PASSWORD})
            if status != 200 or not isinstance(body, dict) or not body.get('AccessToken'):
                raise SystemExit('cannot log in as %s (%s) - run ./scripts/dev.sh up, which seeds the test users' % (user, status))
            self._tokens[user] = body['AccessToken']
        return self._tokens[user]

    def _fixtures(self):
        admin = self.token(ADMIN)
        _, games = self.api('GET', '/api/games', admin)
        now = dt.datetime.now(dt.timezone.utc)

        def closes(game):
            stamp = re.sub(r'(\.\d{6})\d+', r'\1', game['CloseTime']).replace('Z', '+00:00')
            return dt.datetime.fromisoformat(stamp)

        open_games = [g['GameId'] for g in games if g.get('IsOpen') and closes(g) > now + dt.timedelta(minutes=10)]
        pending = [g['GameId'] for g in games if g.get('IsPendingUpdate')]
        if not open_games or not pending:
            print('smoke needs a game open for betting (closing 10+ minutes out) and one pending a result.\n'
                  'The LocalDev fixtures are relative to seed time and have aged out - run ./scripts/dev.sh reset')
            sys.exit(2)
        _, teams = self.api('GET', '/api/teams', admin)
        _, stadiums = self.api('GET', '/api/stadiums', admin)
        f = Fixtures()
        f.open_game, f.open_games, f.pending_game = open_games[0], open_games, pending[0]
        f.team, f.stadium = teams[0]['TeamId'], stadiums[0]['StadiumId']
        return f

    # ---------------------------------------------------------------- page helpers

    def seed(self, user):
        """A fresh session as `user` (None = signed out). The token is planted on a
        same-origin page that does not boot the app, then the app is loaded with it."""
        self.b.desktop()
        self.b.set_rules([])
        self.b.navigate(self.base + '/favicon.ico')
        plant = 'localStorage.accessToken = %s;' % json.dumps(self.token(user)) if user else ''
        self.b.eval('localStorage.clear(); sessionStorage.clear(); %s true' % plant)

    def wait_stable(self, timeout_ms=10000):
        return self.b.eval(WAIT_STABLE % {'inj': INJ, 'timeout': timeout_ms}, timeout=timeout_ms / 1000 + 20)

    def goto(self, path):
        self.b.navigate(self.base + path)
        return self.wait_stable()

    def nav(self, path, settle=True):
        self.b.eval("(function () { var i = %s; i.get('$rootScope').$apply(function () { i.get('$location').path(%s); }); return true; })()"
                    % (INJ, json.dumps(path)))
        return self.wait_stable() if settle else None

    def state(self):
        return self.b.eval(PAGE_STATE % {'inj': INJ})

    def toasts(self):
        return self.b.eval('window.__smokeToasts || []') or []

    def mark(self):
        return (len(self.b.events), len(self.b.console), len(self.b.exceptions), len(self.b.responses))

    def events_since(self, mark):
        return self.b.events[mark[0]:]

    def console_since(self, mark):
        return self.b.console[mark[1]:]

    def exceptions_since(self, mark):
        return self.b.exceptions[mark[2]:]

    def responses_since(self, mark):
        return self.b.responses[mark[3]:]

    def wait_events(self, mark, done, timeout=6.0):
        """Poll until done(events) is true; the SDK sends asynchronously."""
        end = time.time() + timeout
        while time.time() < end:
            if done(self.events_since(mark)):
                break
            time.sleep(0.1)
        return self.events_since(mark)

    def page_problems(self, mark, label, expected_route, settled):
        """Everything wrong with the page as it stands: not settled, wrong route, nothing
        rendered, still behind the overlay, anything reported, logged or failed since mark."""
        time.sleep(0.3)  # give an event raised at the very end of the load time to be sent
        st = self.state() or {}
        problems = []
        if settled != 'stable':
            problems.append('%s: never settled (%s)' % (label, settled))
        if st.get('route') != expected_route:
            problems.append('%s: on route %s, expected %s' % (label, st.get('route'), expected_route))
        if not st.get('viewText'):
            problems.append('%s: ng-view rendered nothing' % label)
        if st.get('hidden'):
            problems.append('%s: content still hidden behind the loading overlay' % label)
        for ev in self.events_since(mark):
            problems.append('%s: Sentry event %s' % (label, ev.describe()))
        for text in self.console_since(mark):
            problems.append('%s: console.error %s' % (label, text[:200]))
        for text in self.exceptions_since(mark):
            problems.append('%s: uncaught %s' % (label, (text or '')[:200]))
        for method, url, status in self.responses_since(mark):
            # A request for a literal {{expression}} is S14's to report, on every route.
            if url.startswith(self.base) and status >= 400 and '%7B%7B' not in url:
                problems.append('%s: %s %s -> %s' % (label, method, url[len(self.base):], status))
        return problems


# ==================================================================== scenarios

SCENARIOS = []


class Scenario:
    def __init__(self, sid, name, fn, fixed_by, reproduces):
        self.sid, self.name, self.fn, self.fixed_by = sid, name, fn, fixed_by
        self.reproduces = re.compile(reproduces, re.S) if reproduces else None


def scenario(sid, name, fixed_by=None, reproduces=None):
    """fixed_by names the change in FIXES that makes a bug scenario pass; invariants have
    none. reproduces is what its failure must say on a baseline run."""
    assert fixed_by is None or (fixed_by in FIXES and reproduces), sid

    def register(fn):
        SCENARIOS.append(Scenario(sid, name, fn, fixed_by, reproduces))
        return fn
    return register


def src_text(path):
    return path.read_text(encoding='utf-8-sig')


@scenario('S0', 'the app serves the committed bundles, freshly built from the sources')
def served_bundles(t):
    status, page = t.http('/')
    check(status == 200, 'GET / answered %s' % status)
    served = re.findall(r'(?:lib/lib|js/app)-(?:min-)?[a-f0-9]+\.js', page)
    check(len(served) == 2, 'expected one lib and one app bundle in the page, found %s' % served)
    cshtml = src_text(APP_DIR / 'Views/Home/Index.cshtml')
    for name in served:
        check(name in cshtml, '%s is served but is not in Index.cshtml on disk - the running app predates the last '
                              'gulp build; ./scripts/dev.sh restart' % name)
        check(t.http('/' + name)[0] == 200, '%s does not load' % name)
    kind = 'min' if all('-min-' in n for n in served) else 'dev' if not any('-min-' in n for n in served) else 'mixed'
    check(kind == t.expect_bundle, 'serving %s bundles, expected %s' % (kind, t.expect_bundle))

    # Every script the page loads from us - the Sentry bundle too - is there, and one pinned
    # with integrity= is byte for byte that file: the browser refuses to run it otherwise.
    for attrs in re.findall(r'<script\b([^>]*)>', page):
        src = re.search(r'\bsrc="([^"]+)"', attrs)
        if not src or re.match(r'(https?:)?//', src.group(1)):
            continue
        try:
            with urllib.request.urlopen(t.base + '/' + src.group(1).lstrip('/'), timeout=20) as r:
                body = r.read()
        except urllib.error.HTTPError as e:
            check(False, '%s does not load (%s)' % (src.group(1), e.code))
        pinned = re.search(r'\bintegrity="sha384-([^"]+)"', attrs)
        if pinned:
            check(base64.b64encode(hashlib.sha384(body).digest()).decode() == pinned.group(1),
                  '%s does not match its integrity attribute, so the browser will not run it' % src.group(1))

    for folder, stem in (('js', 'app'), ('lib', 'lib')):
        names = sorted(p.name for p in (APP_DIR / 'wwwroot' / folder).glob(stem + '-*.js'))
        check(len(names) == 2 and all(n in cshtml for n in names),
              'wwwroot/%s holds %s - not the one gulp build Index.cshtml references' % (folder, names))

    # The dev bundles are plain concatenations, so a source file missing from them verbatim
    # was edited after the last gulp run. The min bundles come out of the same run.
    app_dev = next(p for p in (APP_DIR / 'wwwroot/js').glob('app-*.js') if '-min-' not in p.name).read_text(encoding='utf-8')
    lib_dev = next(p for p in (APP_DIR / 'wwwroot/lib').glob('lib-*.js') if '-min-' not in p.name).read_text(encoding='utf-8')
    stale = [str(p.relative_to(APP_DIR)) for p in sorted((APP_DIR / 'Client/src').rglob('*.js')) if src_text(p) not in app_dev]
    security = APP_DIR / 'Client/lib/angular-spa-security.js'
    if src_text(security) not in lib_dev:
        stale.append(str(security.relative_to(APP_DIR)))
    bom = b'\xef\xbb\xbf'  # gulp strips it on copy
    for tpl in sorted((APP_DIR / 'Client/src').rglob('*.html')):
        copied = APP_DIR / 'wwwroot/App' / tpl.relative_to(APP_DIR / 'Client/src')
        if not copied.exists() or copied.read_bytes().removeprefix(bom) != tpl.read_bytes().removeprefix(bom):
            stale.append(str(tpl.relative_to(APP_DIR)))
    check(not stale, 'changed since the last gulp build - run ./scripts/dev.sh client: %s' % ', '.join(stale[:10]))


def signed_in_routes(t, user):
    other = PLAYER if user != PLAYER else ADMIN
    routes = [
        ('/', '/'), ('/bets_center', '/bets_center'),
        ('/users/' + user, '/users/:username'), ('/users/' + other, '/users/:username'),
        ('/games', '/games'), ('/games/%s' % t.f.open_game, '/games/:gameId'),
        ('/games/%s' % t.f.pending_game, '/games/:gameId'),
        ('/teams', '/teams'), ('/teams/%s' % t.f.team, '/teams/:teamId'),
        ('/stadiums', '/stadiums'), ('/stadiums/%s' % t.f.stadium, '/stadiums/:stadiumId'),
        ('/manage', '/manage'),
    ]
    # A player sent to the admin page is turned back home by its resolve.
    routes.append(('/manage_users', '/manage_users' if user == ADMIN else '/'))
    return routes


@scenario('S1', 'every signed-in route renders cleanly for an admin, a player and a disabled account')
def routes_render(t):
    problems = []
    for user in (ADMIN, PLAYER, DISABLED):
        t.seed(user)
        for path, route in signed_in_routes(t, user):
            mark = t.mark()
            settled = t.goto(path)
            problems += t.page_problems(mark, '%s %s (hard load)' % (user, path), route, settled)
        t.goto('/')
        for path, route in signed_in_routes(t, user):
            mark = t.mark()
            settled = t.nav(path)
            problems += t.page_problems(mark, '%s %s (in-app)' % (user, path), route, settled)
    check(not problems, '\n'.join(problems))


@scenario('S10', 'a rejected bet reports why it was rejected, not [Object]',
          fixed_by='reporting', reproduces=r'bodyJson')
def readable_400(t):
    t.seed(PLAYER)
    t.goto('/')
    mark = t.mark()
    status = t.b.eval("%s.get('$http').put('api/games/%s/mybet', {HomeScore: 11, AwayScore: 1, CardsMark: '1', "
                      "CornersMark: '2'}).then(function () { return 'saved'; }, function (r) { return r.status; })"
                      % (INJ, t.f.open_game))
    check(status == 400, 'a HomeScore of 11 answered %s, expected 400' % status)
    events = t.wait_events(mark, lambda evs: any(e.tags.get('http.status') == '400' for e in evs))
    ev = next((e for e in events if e.tags.get('http.status') == '400'), None)
    check(ev, 'the 400 was not reported at all')
    check(ev.fingerprint == ('http', '400', 'PUT', 'api/games/:id/mybet'), 'fingerprint %s' % list(ev.fingerprint or []))
    response = ev.contexts.get('response') or {}
    check('between 0 and 10' in (response.get('bodyJson') or ''),
          'no readable bodyJson in the reported context: %s' % json.dumps(response)[:300])
    served = re.search(r'js/(app-(?:min-)?[a-f0-9]+\.js)', t.http('/')[1]).group(1)
    check(ev.tags.get('app.bundle') == served, 'app.bundle tag is %r, expected %r' % (ev.tags.get('app.bundle'), served))


@scenario('S11', 'a dropped connection is one network-failure issue, not a fan-out',
          fixed_by='reporting', reproduces=r'outside the network-failure fingerprint')
def network_fanout(t):
    t.seed(PLAYER)
    t.goto('/teams')
    mark = t.mark()
    # The dashboard's own requests die (its route resolve is already cached by /teams)...
    t.b.set_rules([{'pattern': '*/api/games*', 'action': 'fail'},
                   {'pattern': '*/api/users*', 'action': 'fail'},
                   {'pattern': '*/api/generalbets*', 'action': 'fail'}])
    t.nav('/', settle=False)
    time.sleep(2.5)
    # ...then a route whose template never arrives.
    t.b.set_rules([{'pattern': '*/App/Stadiums/Stadiums.html*', 'action': 'fail'}])
    t.nav('/stadiums', settle=False)
    time.sleep(2.5)
    t.b.set_rules([])
    events = t.wait_events(mark, lambda evs: False, timeout=1.0)
    check(events, 'nothing was reported for the dropped connection')
    wrong = [e for e in events if e.fingerprint != ('http', 'network-failure') or e.level != 'warning']
    check(not wrong, 'events outside the network-failure fingerprint:\n' + '\n'.join('  ' + e.describe() for e in wrong))
    untagged = [e for e in events if not e.tags.get('http.url') or e.tags.get('http.xhrStatus') not in ('error', 'timeout', 'abort')
                or 'document.visibilityState' not in e.tags]
    check(not untagged, 'network failures missing the endpoint/xhrStatus/visibility tags:\n'
          + '\n'.join('  %s %s' % (e.describe(), e.tags) for e in untagged))
    check(any(x.get('title') == 'Connection Problem' for x in t.toasts()), 'the user was not told the connection failed')


# Every user-keyed endpoint the client calls, as (method, path, the route it must group as).
# {u}/{v} are usernames, {g} an account GUID.
USER_KEYED = [
    ('GET', 'api/users/{u}', 'api/users/:user'),
    ('GET', 'api/users/{u}/followees', 'api/users/:user/followees'),
    ('GET', 'api/users/{u}/followers', 'api/users/:user/followers'),
    ('POST', 'api/users/follow/{u}', 'api/users/follow/:user'),
    ('DELETE', 'api/users/follow/{u}', 'api/users/follow/:user'),
    ('POST', 'api/users/makeadmin/{g}', 'api/users/makeadmin/:user'),
    ('POST', 'api/users/{g}/activate', 'api/users/:user/activate'),
    ('DELETE', 'api/users/{g}/activate', 'api/users/:user/activate'),
    ('DELETE', 'api/users/{g}', 'api/users/:user'),
    ('GET', 'api/users/compare/{u}/{v}', 'api/users/compare/:user/:user'),
    ('GET', 'api/bets/user/{u}', 'api/bets/user/:user'),
    ('GET', 'api/generalbets/has-bet/{u}', 'api/generalbets/has-bet/:user'),
    ('GET', 'api/generalbets/user/{u}', 'api/generalbets/user/:user'),
    ('GET', 'api/stats/{u}', 'api/stats/:user'),
]
# The caller's own endpoints are routes, not users, and must stay literal.
SELF_ROUTES = [('GET', 'api/users/me'), ('GET', 'api/users/me/progress'), ('GET', 'api/stats/me')]


@scenario('S12', 'a failure on a user-keyed endpoint groups by route, not by user',
          fixed_by='reporting', reproduces=r'fingerprint .*(alice|bob|carol|[0-9a-f]{8}-)')
def username_grouping(t):
    t.seed(PLAYER)
    t.goto('/')
    people = [dict(u='alice.smoke', v='carol_smoke', g=str(uuid.uuid4())),
              dict(u='bob-smoke', v='dave.smoke', g=str(uuid.uuid4()))]
    batches = [[(m, p.format(**who), shape) for m, p, shape in USER_KEYED] for who in people]
    batches.append([(m, p, p) for m, p in SELF_ROUTES])
    # Every request is answered here with a fake 500 and never reaches the server, so
    # nobody is followed, promoted or deleted in the dev database.
    t.b.set_rules([{'pattern': '*/api/users/*', 'action': 'fulfill', 'status': 500, 'body': '{}'},
                   {'pattern': '*/api/bets/user/*', 'action': 'fulfill', 'status': 500, 'body': '{}'},
                   {'pattern': '*/api/generalbets/*', 'action': 'fulfill', 'status': 500, 'body': '{}'},
                   {'pattern': '*/api/stats/*', 'action': 'fulfill', 'status': 500, 'body': '{}'}])
    mark = t.mark()
    # In batches: the v6 transport holds at most 30 events in flight and silently drops the
    # rest ("Not adding Promise due to buffer limit reached").
    sent = 0
    for batch in batches:
        t.b.eval("(function () { var i = %s, $http = i.get('$http'), $q = i.get('$q');"
                 " return $q.all(%s.map(function (c) { return $http({method: c[0], url: c[1]}).catch(angular.noop); }))"
                 ".then(function () { return true; }); })()" % (INJ, json.dumps([[m, u] for m, u, _ in batch])))
        sent += len(batch)
        t.wait_events(mark, lambda evs: len(evs) >= sent, timeout=8)
    calls = [call for batch in batches for call in batch]
    events = t.events_since(mark)
    t.b.set_rules([])
    by_call = {}
    for e in events:
        response = e.contexts.get('response') or {}
        by_call[(response.get('method'), response.get('url'))] = e
    problems = []
    for method, url, shape in calls:
        ev = by_call.get((method, url))
        expected = ('http', '500', method, shape)
        if ev is None:
            problems.append('%s %s: not reported' % (method, url))
        elif ev.fingerprint != expected:
            problems.append('%s %s: fingerprint %s, expected %s' % (method, url, list(ev.fingerprint or []), list(expected)))
    check(not problems, '\n'.join(problems))


@scenario('S13', 'real errors still reach Sentry - the filters are not too wide')
def still_reported(t):
    t.seed(PLAYER)
    t.goto('/')
    n = uuid.uuid4().hex[:8]
    t.b.set_rules([{'pattern': '*/api/smoke-500-*', 'action': 'fulfill', 'status': 500, 'body': '{}'}])
    mark = t.mark()
    t.b.eval(r"""(function () {
      var i = %(inj)s, $q = i.get('$q'), n = %(n)s;
      i.get('$rootScope').$apply(function () {
        $q.reject(new Error('smoke-error-' + n));
        $q.reject('smoke-string-' + n);
        i.get('$timeout')(function () { var nothing = null; return nothing['smoke' + n]; });
        i.get('$templateRequest')('App/smoke-missing-' + n + '.html').catch(angular.noop);
        i.get('$http').get('api/smoke-500-' + n).catch(angular.noop);
      });
      setTimeout(function () { throw new Error('smoke-global-' + n); });
      return true;
    })()""" % {'inj': INJ, 'n': json.dumps(n)})
    expected = {
        'an unhandled rejection carrying an Error': lambda e: any('smoke-error-' + n in v for _, v in e.exceptions),
        'an unhandled rejection carrying a string': lambda e: 'smoke-string-' + n in e.title,
        'a TypeError inside a $timeout': lambda e: any(k == 'TypeError' and 'smoke' + n in v for k, v in e.exceptions),
        'an uncaught error outside Angular': lambda e: any('smoke-global-' + n in v for _, v in e.exceptions),
        'a template that 404s': lambda e: 'smoke-missing-' + n in e.title and 'Failed to load template' in e.title,
        'an HTTP 500': lambda e: 'api/smoke-500-' + n in e.title,
    }
    events = t.wait_events(mark, lambda evs: all(any(f(e) for e in evs) for f in expected.values()), timeout=8)
    t.b.set_rules([])
    missing = [what for what, found in expected.items() if not any(found(e) for e in events)]
    check(not missing, 'not reported: %s\nreported: %s' % ('; '.join(missing), '\n  '.join(e.describe() for e in events)))


@scenario('S16', "Angular's own errors reach Sentry in Angular's shape, and Angular still logs them")
def angular_error_shape(t):
    # What the SDK's AngularJS integration (ngSentry, gone since SDK v7) did, and what
    # SentryExceptionHandler.js must keep doing: '[$module:code] message\n<docs url>' becomes
    # type '$module:code' and value 'message' with the url in extra.angularDocs; the element
    # whose directive threw arrives as extra.cause; and Angular's own handler still runs, so
    # the error is logged too.
    t.seed(PLAYER)
    t.goto('/')
    n = uuid.uuid4().hex[:8]
    mark = t.mark()
    t.b.eval(r"""(function () {
      var i = %(inj)s, $rootScope = i.get('$rootScope'), n = %(n)s;
      // $apply from a task the digest is running: [$rootScope:inprog]
      $rootScope.$evalAsync(function () { $rootScope.$apply(); });
      // an expression that throws while its element is linked
      var scope = $rootScope.$new();
      scope.smokeBoom = function () { throw new Error('smoke-link-' + n); };
      i.get('$compile')('<div ng-init="smokeBoom()"></div>')(scope);
      return true;
    })()""" % {'inj': INJ, 'n': json.dumps(n)})

    def inprog(e):
        return any(k == '$rootScope:inprog' and v.startswith('$digest already in progress') for k, v in e.exceptions)

    def linked(e):
        return any('smoke-link-' + n in v for _, v in e.exceptions)

    events = t.wait_events(mark, lambda evs: any(inprog(e) for e in evs) and any(linked(e) for e in evs))
    shown = '\n  '.join('%s extra=%s' % (e.describe(), json.dumps(e.extra)[:160]) for e in events) or 'nothing'
    ev = next((e for e in events if inprog(e)), None)
    check(ev, 'no $rootScope:inprog event with the Angular code as its type; reported:\n  ' + shown)
    check(str(ev.extra.get('angularDocs', '')).startswith('https://errors.angularjs.org/'),
          'the inprog event has no angularDocs link: extra=%s' % json.dumps(ev.extra)[:300])
    ev = next((e for e in events if linked(e)), None)
    check(ev, 'the error thrown while linking was not reported; reported:\n  ' + shown)
    check('ng-init="smokeBoom()"' in str(ev.extra.get('cause', '')),
          'the linking error has no cause naming its element: extra=%s' % json.dumps(ev.extra)[:300])
    logged = t.console_since(mark)
    for what in ('$rootScope:inprog', 'smoke-link-' + n):
        check(any(what in line for line in logged), "Angular's handler did not log %s - console: %s" % (what, logged[:5]))


@scenario('S17', 'the app still loads when the Sentry script does not',
          fixed_by='sdk-v10', reproduces=r'modulerr')
def sentry_script_blocked(t):
    # With SDK v6 the app module depended on ngSentry, which existed only once both Sentry
    # scripts had loaded and Sentry.init had run: if either failed to load, Angular could
    # not boot at all. Sentry must never again be able to take the app down.
    t.seed(PLAYER)
    t.b.set_rules([{'pattern': '*/sentry/*', 'action': 'fail'}])
    mark = t.mark()
    settled = t.goto('/games')
    problems = t.page_problems(mark, '/games with the Sentry script blocked', '/games', settled)
    t.b.set_rules([])
    check(not problems, '\n'.join(problems))


def on_scope(prop, body):
    """Run `body` inside $apply on the view scope that owns `prop` (bound as `s`). Owned, not
    inherited: writing through a child scope - an ng-if, a uib-tab - would shadow it."""
    return r"""(function () {
      var els = document.querySelectorAll('[ng-view] *'), s = null;
      for (var i = 0; i < els.length && !s; i++) {
        var c = angular.element(els[i]).scope();
        while (c && !Object.prototype.hasOwnProperty.call(c, %(prop)s)) c = c.$parent;
        s = c;
      }
      if (!s) throw new Error('no scope owns ' + %(prop)s);
      s.$apply(function () { %(body)s });
      return true;
    })()""" % {'prop': json.dumps(prop), 'body': body}


def centre_of(t, locate_js):
    """Scroll the element `locate_js` evaluates to into view and return its centre."""
    return t.b.eval(r"""(function () {
      var el = %s;
      if (!el) return null;
      el.scrollIntoView({block: 'center', inline: 'center'});
      var r = el.getBoundingClientRect();
      return {x: r.left + r.width / 2, y: r.top + r.height / 2};
    })()""" % locate_js)


def grid_header(icon):
    return (r"""(function () {
      var cells = document.querySelectorAll('.ui-grid-header-cell');
      for (var i = 0; i < cells.length; i++)
        if (cells[i].querySelector(%s)) return cells[i].querySelector('.ui-grid-cell-contents');
      return null;
    })()""" % json.dumps(icon))


def sort_direction(t, field):
    return t.b.eval(r"""(function () {
      var grids = document.querySelectorAll('.ui-grid');
      for (var i = 0; i < grids.length; i++) {
        var ctrl = angular.element(grids[i]).controller('uiGrid');
        var col = ctrl && ctrl.grid.columns.filter(function (c) { return c.field === %s; })[0];
        if (col) return (col.sort && col.sort.direction) || 'none';
      }
      return 'no grid';
    })()""" % json.dumps(field))


def sort_by_header_clicks(t, where):
    problems = []
    mark = t.mark()
    for field, icon in (('YellowCards', '.fa-stop'), ('Corners', '.fa-flag')):
        seen = []
        for _ in range(2):
            point = centre_of(t, grid_header(icon))
            if not point:
                problems.append('%s: no %s header to click' % (where, field))
                break
            t.b.click_at(point['x'], point['y'])
            time.sleep(0.5)
            seen.append(sort_direction(t, field))
        if seen and seen != ['asc', 'desc']:
            problems.append('%s: clicking %s sorted it %s, expected asc then desc' % (where, field, seen))
    for ev in t.wait_events(mark, lambda evs: False, timeout=1.0):
        problems.append('%s: Sentry event %s' % (where, ev.describe()))
    return problems


@scenario('S6', 'leaderboard headers sort without throwing',
          fixed_by='bet-forms', reproduces=r'sort is not a function')
def grid_sort(t):
    t.seed(PLAYER)
    t.goto('/')
    t.b.eval(on_scope('tableToggleValue', 's.tableToggleValue = true;'))
    t.wait_stable()
    problems = sort_by_header_clicks(t, 'dashboard leaderboard')
    # The simulated ranking of a game awaiting its result uses the same column templates.
    t.goto('/games/%s' % t.f.pending_game)
    t.b.eval(on_scope('simulatedGame', "s.gameActiveTab = 1; s.simulatedGame.HomeScore = 2; s.simulatedGame.AwayScore = 1;"
                                       " s.simulatedGame.CardsMark = '1'; s.simulatedGame.CornersMark = '2'; s.simulateGame();"))
    t.wait_stable()
    problems += sort_by_header_clicks(t, 'simulated ranking')
    check(not problems, '\n'.join(problems))


def check_bet_form(t, where, form_js, fill, press):
    """Drive one bet form: what Save allows for bad home scores, then a real save.

    form_js evaluates to {home, save} elements; fill() makes every field valid; press(point)
    is a real click or tap. Each bad value is checked on its own, from a valid form."""
    problems = []
    setup = r"""(function () { var f = %s; if (!f || !f.home || !f.save) return false;
      window.__smokeForm = f; return true; })()""" % form_js
    check(t.b.eval(setup), '%s: bet form not found' % where)
    set_home = r"""(function (v) { var f = window.__smokeForm; f.home.value = v;
      f.home.dispatchEvent(new Event('input', {bubbles: true})); return f.save.disabled; })(%s)"""
    fill()
    check(not t.b.eval('window.__smokeForm.save.disabled'), '%s: Save is disabled on a complete bet' % where)
    for bad in ('', '11', '1.5'):
        if not t.b.eval(set_home % json.dumps(bad)):
            problems.append('%s: Save stays enabled with HomeScore=%r' % (where, bad))
    if t.b.eval(set_home % json.dumps('3')):
        problems.append('%s: Save is disabled with HomeScore=3' % where)
    mark = t.mark()
    point = centre_of(t, 'window.__smokeForm.save')
    press(point['x'], point['y'])
    end = time.time() + 6
    saved = None
    while time.time() < end and saved is None:
        saved = next((s for m, u, s in t.responses_since(mark) if m == 'PUT' and u.endswith('/mybet')), None)
        time.sleep(0.1)
    if saved not in (200, 201):
        problems.append('%s: saving HomeScore=3 answered %s' % (where, saved))
    else:
        time.sleep(0.3)
        if not any(x.get('body') == 'Bet was saved successfully' for x in t.toasts()):
            problems.append('%s: saved, but the user was not told' % where)
    for ev in t.events_since(mark):
        problems.append('%s: Sentry event %s' % (where, ev.describe()))
    return problems


BETS_CENTER_ROW = r"""(function () {
  var rows = document.querySelectorAll('table.mu-bets-center-table__grid tbody tr');
  for (var i = 0; i < rows.length; i++) {
    var s = angular.element(rows[i]).scope();
    if (s && s.game && s.game.GameId === %(game)d)
      return {home: rows[i].querySelector('input[name=homeScore]'), save: rows[i].querySelector('button[title=Save]'),
              shuffle: rows[i].querySelector('button[title=Shuffle]')};
  }
  return null;
})()"""

BETS_CENTER_CARD = r"""(function () {
  var cards = document.querySelectorAll('.mu-bets-center-card');
  for (var i = 0; i < cards.length; i++) {
    var s = angular.element(cards[i]).scope();
    if (s && s.game && s.game.GameId === %(game)d)
      return {home: cards[i].querySelector('input[name=homeScoreMobile]'),
              save: cards[i].querySelector('.mu-bets-center-card__actions .btn-success'),
              shuffle: cards[i].querySelector('.mu-bets-center-card__actions .btn-info')};
  }
  return null;
})()"""


@scenario('S7', 'Bets Center (desktop): Save refuses an empty or out-of-range score',
          fixed_by='bet-forms', reproduces=r'Save stays enabled')
def bet_bounds_desktop(t):
    t.seed(PLAYER)
    t.goto('/bets_center')
    problems = check_bet_form(t, 'bets center row', BETS_CENTER_ROW % {'game': t.f.open_game},
                              fill=lambda: t.b.eval('window.__smokeForm.shuffle.click(); true'),
                              press=t.b.click_at)
    check(not problems, '\n'.join(problems))


@scenario('S8', 'Bets Center (phone): Save refuses an out-of-range score',
          fixed_by='bet-forms', reproduces=r'Save stays enabled')
def bet_bounds_mobile(t):
    t.seed(PLAYER)
    t.b.mobile()
    t.goto('/bets_center')
    problems = check_bet_form(t, 'bets center card', BETS_CENTER_CARD % {'game': t.f.open_game},
                              fill=lambda: t.b.eval('window.__smokeForm.shuffle.click(); true'),
                              press=t.b.tap_at)
    t.b.desktop()
    check(not problems, '\n'.join(problems))


@scenario('S9', 'game page: Save refuses an out-of-range score',
          fixed_by='bet-forms', reproduces=r'Save stays enabled')
def bet_bounds_game(t):
    t.seed(PLAYER)
    t.goto('/games/%s' % t.f.open_game)
    form = r"""(function () { var f = document.querySelector('form[name=userBetFrom]');
      return f && {home: f.querySelector('input[ng-model="userBet.HomeScore"]'), save: f.querySelector('button.btn-primary')}; })()"""

    def fill():
        t.b.eval(on_scope('userBet', "s.userBet.HomeScore = 2; s.userBet.AwayScore = 1;"
                                     " s.userBet.CardsMark = '1'; s.userBet.CornersMark = '2';"))
    problems = check_bet_form(t, 'game page', form, fill=fill, press=t.b.click_at)
    check(not problems, '\n'.join(problems))


@scenario('S14', 'no page requests an unbound {{expression}} as a URL',
          fixed_by='bet-forms', reproduces=r'profileUser\.ProfilePicture')
def unbound_urls(t):
    t.seed(PLAYER)
    bad = set()
    for path, _ in signed_in_routes(t, PLAYER):
        mark = t.mark()
        t.goto(path)
        time.sleep(0.3)
        for method, url, status in t.responses_since(mark):
            if '%7B%7B' in url:
                bad.add('%s requested %s (%s)' % (path, urllib.parse.unquote(url[len(t.base):]), status))
    check(not bad, '\n'.join(sorted(bad)))


USER_INFO = '*/api/account/userInfo*'


def sign_in_on_page(t, user):
    """Sign in through the login page's own controller. Two digests on purpose: login()
    checks loginForm.$valid, which only sees the credentials once a digest has run."""
    t.b.eval(on_scope('login', 's.user.username = %s; s.user.password = %s;' % (json.dumps(user), json.dumps(PASSWORD))))
    t.b.eval(on_scope('login', 's.login();'))
    time.sleep(0.5)
    return t.wait_stable()


@scenario('S2', 'a page whose data beats the user load does not run without a user',
          fixed_by='user-guard', reproduces=r'Followees')
def slow_user_load(t):
    # The user arrives with one request at startup; hold it back and every signed-in page
    # below resolves its own data first. GameCtrl reads Followees only for a closed game.
    problems = []
    for path, route in (('/', '/'), ('/users/' + ADMIN, '/users/:username'), ('/games/%s' % t.f.pending_game, '/games/:gameId')):
        t.seed(PLAYER)
        t.b.set_rules([{'pattern': USER_INFO, 'action': 'hold', 'seconds': 2.0, 'times': 1}])
        mark = t.mark()
        settled = t.goto(path)
        problems += t.page_problems(mark, '%s with a slow user load' % path, route, settled)
        if path == '/':
            problems += dashboard_stats_problems(t, '/ with a slow user load')
    check(not problems, '\n'.join(problems))


def dashboard_stats_problems(t, what):
    """The dashboard fails silently without a user: its Followees read throws inside the
    games promise, which cg-busy holds with an error handler, so nothing is reported - the
    pending game's bet stats just never appear."""
    keys = t.b.eval(r"""(function () {
      var els = document.querySelectorAll('[ng-view] *');
      for (var i = 0; i < els.length; i++) {
        var s = angular.element(els[i]).scope();
        if (s && s.resultsDic) return Object.keys(s.resultsDic);
      }
      return null;
    })()""")
    if str(t.f.pending_game) in (keys or []):
        return []
    return ['%s: the pending game %s never got its bet stats (dashboard resultsDic keys %s)' % (what, t.f.pending_game, keys)]


def failed_user_load(t):
    """A hard load whose first user request dies without an answer, as on a phone that
    just woke. Returns the page problems, less the network failure that is expected."""
    t.seed(PLAYER)
    t.b.set_rules([{'pattern': USER_INFO, 'action': 'fail', 'times': 1}])
    mark = t.mark()
    settled = t.goto('/users/' + ADMIN)
    time.sleep(0.5)
    problems = t.page_problems(mark, 'profile after a failed user load', '/users/:username', settled)
    expected = 'fingerprint=%s' % list(('http', 'network-failure'))
    return [p for p in problems if not (('Sentry event' in p and expected in p) or 'userInfo' in p)]


@scenario('S3a', 'a failed user load does not run the page without a user',
          fixed_by='user-guard', reproduces=r'Followees')
def failed_user_load_no_crash(t):
    problems = [p for p in failed_user_load(t) if 'Sentry event' in p or 'uncaught' in p]
    check(not problems, '\n'.join(problems))


@scenario('S3b', 'a failed user load is retried, so the page still loads without a reload',
          fixed_by='user-guard', reproduces=r'hidden behind the loading overlay')
def failed_user_load_recovers(t):
    problems = [p for p in failed_user_load(t) if 'Sentry event' not in p]
    check(not problems, '\n'.join(problems))


@scenario('S4', 'a signed-out deep link goes to the login page, then back to the link')
def signed_out_deep_link(t):
    t.seed(None)
    target = '/users/' + ADMIN
    t.goto(target)
    check((t.state() or {}).get('path') == '/login', 'a signed-out visit to %s did not go to the login page' % target)
    mark = t.mark()
    settled = sign_in_on_page(t, PLAYER)
    problems = t.page_problems(mark, 'after signing in', '/users/:username', settled)
    check(t.state().get('path') == target, 'signing in landed on %s, expected %s' % (t.state().get('path'), target))
    check(not problems, '\n'.join(problems))


@scenario('S5', 'a 401 on the user load does not trap sign-in on the login page')
def expired_session(t):
    t.seed(PLAYER)
    t.b.set_rules([{'pattern': USER_INFO, 'action': 'fulfill', 'status': 401, 'body': '{}', 'times': 1}])
    t.goto('/games')
    check((t.state() or {}).get('path') == '/login', 'a 401 on the user load did not go to the login page')
    mark = t.mark()
    settled = sign_in_on_page(t, PLAYER)
    landed = t.state().get('path')
    check(landed != '/login', 'signing in after a 401 stayed on the login page')
    problems = t.page_problems(mark, 'after signing in', '/', settled)
    check(not problems, '\n'.join(problems))


@scenario('S15', 'signing out and back in returns to the page, with the user loaded')
def sign_out_and_in(t):
    # The logout handler (app.js) calls authenticate(), which records the page signed out
    # from - so signing back in returns there, not to the dashboard.
    t.seed(PLAYER)
    t.goto('/games')
    t.b.eval("(function () { var i = %s; i.get('$rootScope').$apply(function () { i.get('security').logout(); }); return true; })()" % INJ)
    t.wait_stable()
    st = t.state() or {}
    check(st.get('path') == '/login' and st.get('user') is None, 'signing out left %s' % st)
    check(not t.b.eval('localStorage.accessToken || sessionStorage.accessToken || null'), 'signing out kept the token')
    mark = t.mark()
    settled = sign_in_on_page(t, PLAYER)
    problems = t.page_problems(mark, 'after signing back in', '/games', settled)
    check(t.state().get('user') == PLAYER, 'signed back in, but security.user is %r' % t.state().get('user'))
    check(not problems, '\n'.join(problems))


# ==================================================================== runner

def expected_to_fail(sc, baseline):
    return bool(baseline and sc.fixed_by and FIXES.index(sc.fixed_by) >= FIXES.index(baseline))


def save_artifacts(t, sc, mark):
    ARTIFACTS.mkdir(parents=True, exist_ok=True)
    stem = ARTIFACTS / ('%s-%s' % (sc.sid, time.strftime('%H%M%S')))
    try:
        t.b.screenshot(str(stem) + '.png')
    except Exception:
        pass
    dump = {'events': [e.raw for e in t.events_since(mark)], 'console': t.console_since(mark),
            'exceptions': t.exceptions_since(mark), 'responses': t.responses_since(mark)}
    pathlib.Path(str(stem) + '.json').write_text(json.dumps(dump, indent=1, default=str))
    return stem


def main():
    parser = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    parser.add_argument('--base', default='http://localhost:5150')
    parser.add_argument('--baseline', choices=FIXES,
                        help='run against the code before this change: its bug scenarios (and later ones) must fail')
    parser.add_argument('--expect-bundle', choices=['dev', 'min'],
                        help='which bundles the app should be serving (default: from .dev/mode)')
    parser.add_argument('--only', action='append', default=[], help='glob on scenario id or name (repeatable)')
    parser.add_argument('--headful', action='store_true', help='show the browser')
    parser.add_argument('--list', action='store_true', help='list the scenarios and exit')
    args = parser.parse_args()

    selected = [s for s in SCENARIOS if not args.only or any(
        fnmatch.fnmatch(s.sid, g) or fnmatch.fnmatch(s.name, g) for g in args.only)]
    if args.list:
        for s in SCENARIOS:
            print('%-4s %-11s %s' % (s.sid, s.fixed_by or 'invariant', s.name))
        return 0
    if not selected:
        print('no scenario matches %s' % args.only)
        return 2

    expect_bundle = args.expect_bundle
    if not expect_bundle:
        mode = (RUN_DIR / 'mode').read_text().strip() if (RUN_DIR / 'mode').exists() else 'dev'
        expect_bundle = 'min' if mode == 'prod-bundle' else 'dev'

    browser = Browser(headful=args.headful)
    ok = True
    try:
        t = Suite(args.base, browser, expect_bundle)
        print('\nMundialito smoke  %s  %s bundles%s\n' % (
            t.base, expect_bundle, '  baseline: before %s' % args.baseline if args.baseline else ''))
        for sc in selected:
            xfail = expected_to_fail(sc, args.baseline)
            mark = t.mark()
            started = time.time()
            try:
                sc.fn(t)
                outcome, detail = 'pass', ''
            except Failure as e:
                outcome, detail = 'fail', str(e)
            except Exception:
                outcome, detail = 'error', traceback.format_exc()
            took = time.time() - started
            if xfail:
                if outcome == 'fail' and sc.reproduces.search(detail):
                    verdict, good, detail = 'XFAIL', True, 'reproduces: ' + detail
                elif outcome == 'pass':
                    verdict, good, detail = 'XPASS', False, 'passes before %s - it does not reproduce the bug' % sc.fixed_by
                else:
                    verdict, good = 'FAIL', False
                    detail = 'failed, but not with the production symptom /%s/:\n%s' % (sc.reproduces.pattern, detail)
            else:
                verdict, good = ('PASS', True) if outcome == 'pass' else ('FAIL', False)
            print('  %-6s %-4s %s  (%.1fs)' % (verdict, sc.sid, sc.name, took))
            if detail and (not good or verdict == 'XFAIL'):
                for line in detail.strip().splitlines()[:40]:
                    print('         ' + line)
            if not good:
                ok = False
                print('         artifacts: %s.{png,json}' % save_artifacts(t, sc, mark))
            try:
                browser.set_rules([])
            except Exception:
                pass
        if browser.sink_errors:
            ok = False
            print('\n  harness errors while reading Sentry payloads or CDP events:')
            for line in browser.sink_errors[:20]:
                print('    ' + line)
    finally:
        browser.close()
    print('\n' + ('all scenarios behaved as expected' if ok else 'smoke FAILED') + '\n')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
