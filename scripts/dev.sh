#!/usr/bin/env bash
#
# Local development environment for Mundialito.
#
#   ./scripts/dev.sh up       start database + app, seed test users, print a summary
#   ./scripts/dev.sh down     stop the app and the database
#   ./scripts/dev.sh restart  restart the app only, keeping the database
#   ./scripts/dev.sh reset    wipe the database and start again from a fresh seed
#   ./scripts/dev.sh status   what is running
#   ./scripts/dev.sh logs     follow the app log
#   ./scripts/dev.sh verify   API regression checks (roles, bet write path, admin writes)
#   ./scripts/dev.sh client   rebuild the AngularJS bundles with gulp
#   ./scripts/dev.sh smoke    browser smoke suite against the running app (scripts/smoke.py)
#
#   up and restart take --prod-bundle: run as Production so the page serves the minified
#   bundles users actually get. Open that instance only through `smoke` - see app_env.
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APP_DIR="$ROOT/Mundialito"
RUN_DIR="$ROOT/.dev"
LOG="$RUN_DIR/app.log"
PIDFILE="$RUN_DIR/app.pid"
PORT=5150
BASE="http://localhost:$PORT"
CONTAINER=mundialito-pg

ADMIN_USER=roez
ADMIN_PASS=123456
TEST_PASS=123456
DISABLED_USER=dev_disabled
ACTIVE_USER=dev_active

bold() { printf '\033[1m%s\033[0m\n' "$1"; }
info() { printf '  %s\n' "$1"; }
die()  { printf '\033[31merror:\033[0m %s\n' "$1" >&2; exit 1; }

need() { command -v "$1" >/dev/null 2>&1 || die "$1 is required but not installed"; }

# Index.cshtml serves the minified -min bundles only when the environment is Production, so a
# Development run never executes the code production users get - a minifier or DI-annotation
# break is invisible here. --prod-bundle closes that gap.
PROD_BUNDLE=0
parse_mode() { # parse_mode "$@" - the only option up and restart take
  for arg in "$@"; do
    case "$arg" in
      --prod-bundle) PROD_BUNDLE=1 ;;
      *) die "unknown option $arg" ;;
    esac
  done
}

dev_settings_env() { # appsettings.Development.json flattened to NUL-separated KEY__SUB=value
  python3 - "$APP_DIR/appsettings.Development.json" <<'PY'
import json, re, sys
def walk(path, node):
    if isinstance(node, dict):
        for k, v in node.items():
            yield from walk(path + [k], v)
    elif isinstance(node, list):
        for i, v in enumerate(node):
            yield from walk(path + [str(i)], v)
    else:
        yield '__'.join(path), '' if node is None else str(node).lower() if isinstance(node, bool) else str(node)
with open(sys.argv[1], encoding='utf-8-sig') as f:
    for key, value in walk([], json.load(f)):
        # Keys like Logging:LogLevel:Microsoft.AspNetCore are not valid shell names; none matter here.
        if re.fullmatch(r'[A-Za-z_][A-Za-z0-9_]*', key):
            sys.stdout.write(key + '=' + value + '\0')
PY
}

# The tournament window drives registration and the general-bet deadline. Relative to now so
# it never goes stale, unlike the dates committed in appsettings.Development.json.
app_env() {
  if [ "$PROD_BUNDLE" = 1 ]; then
    # appsettings.Development.json is not loaded outside Development, so hand its values -
    # the connection string above all - over as environment variables. First, so the
    # explicit settings below still win.
    while IFS= read -r -d '' kv; do export "${kv?}"; done < <(dev_settings_env)
  fi
  ASPNETCORE_ENVIRONMENT=Development
  App__TournamentDBCreatorName=LocalDev
  App__TournamentStartDate="$(date -u -v+1d '+%d/%m/%Y %H:%M' 2>/dev/null || date -u -d '+1 day' '+%d/%m/%Y %H:%M')"
  App__TournamentEndDate="$(date -u -v+180d '+%d/%m/%Y %H:%M' 2>/dev/null || date -u -d '+180 days' '+%d/%m/%Y %H:%M')"
  # The real signing key is a secret and is not committed; production gets it from an App
  # Service application setting. This throwaway is local-only and signs nothing that matters.
  JwtTokenSettings__SymmetricSecurityKey="${JwtTokenSettings__SymmetricSecurityKey:-local-development-only-do-not-use-anywhere-real}"
  export ASPNETCORE_ENVIRONMENT App__TournamentDBCreatorName App__TournamentStartDate App__TournamentEndDate
  export JwtTokenSettings__SymmetricSecurityKey
  if [ "$PROD_BUNDLE" = 1 ]; then
    export ASPNETCORE_ENVIRONMENT=Production
    export ASPNETCORE_URLS="$BASE"
    # Program.cs wires Application Insights outside Development and rejects an empty
    # connection string. This one is well formed and points at a closed local port.
    export APPLICATIONINSIGHTS_CONNECTION_STRING='InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:9/'
    # appsettings.json carries the real backend DSN, and a local run tagged Production would
    # land in the very Sentry window a deploy is judged by. An empty DSN disables the SDK;
    # Debug makes it say so, which check_prod_bundle insists on before trusting the instance.
    # The browser DSN is hard-coded in Index.cshtml and cannot be switched off from here -
    # which is why a --prod-bundle instance is only ever opened through smoke.py, whose
    # browser answers every sentry.io request itself.
    export Sentry__Dsn=
    export Sentry__Debug=true
    export Sentry__DiagnosticLevel=Debug
  fi
}

app_running() { [ -f "$PIDFILE" ] && kill -0 "$(cat "$PIDFILE")" 2>/dev/null; }
db_running()  { [ -n "$(docker ps -q --filter "name=^${CONTAINER}$")" ]; }

# ---------------------------------------------------------------------------- api helpers

api() { # api <method> <path> [token] [json]
  local method=$1 path=$2 token=${3:-} body=${4:-}
  local args=(-s -X "$method" "$BASE$path" -H 'Content-Type: application/json')
  [ -n "$token" ] && args+=(-H "Authorization: Bearer $token")
  [ -n "$body" ] && args+=(-d "$body")
  curl "${args[@]}"
}

api_code() { # api_code <method> <path> [token] [json] -> status code, body in $API_BODY_FILE
  local method=$1 path=$2 token=${3:-} body=${4:-}
  local args=(-s -o "$RUN_DIR/body" -w '%{http_code}' -X "$method" "$BASE$path" -H 'Content-Type: application/json')
  [ -n "$token" ] && args+=(-H "Authorization: Bearer $token")
  [ -n "$body" ] && args+=(-d "$body")
  curl "${args[@]}"
}

login() { # login <user> <pass> -> access token, empty on failure
  api POST /api/account/login '' "{\"Username\":\"$1\",\"Password\":\"$2\"}" \
    | python3 -c 'import sys,json
try: print(json.load(sys.stdin).get("AccessToken",""))
except Exception: print("")'
}

register() { # register <user> - idempotent
  api POST /api/account/register '' \
    "{\"UserName\":\"$1\",\"Email\":\"$1@example.com\",\"FirstName\":\"Dev\",\"LastName\":\"User\",\"Password\":\"$TEST_PASS\",\"ConfirmPassword\":\"$TEST_PASS\"}" >/dev/null
}

user_id() { # user_id <admin-token> <username>
  api GET /api/users "$1" | python3 -c "import sys,json
print(next((u['Id'] for u in json.load(sys.stdin) if u['Username']=='$2'), ''))"
}

# ---------------------------------------------------------------------------- commands

start_db() {
  if db_running; then
    info "database already running"
  else
    docker compose -f "$ROOT/compose.yml" up -d >/dev/null 2>&1
    printf '  waiting for postgres'
    for _ in $(seq 1 30); do
      if docker exec "$CONTAINER" pg_isready -U mundialito -d mundialito >/dev/null 2>&1; then
        printf ' ready\n'; return
      fi
      printf '.'; sleep 1
    done
    printf '\n'; die "postgres did not become ready - check: docker logs $CONTAINER"
  fi
}

start_app() {
  if app_running; then
    info "app already running (pid $(cat "$PIDFILE"))"
    return
  fi
  # Report a busy port rather than killing whatever owns it - it may not be ours.
  if lsof -ti ":$PORT" >/dev/null 2>&1; then
    die "port $PORT is in use by pid $(lsof -ti ":$PORT" | tr '\n' ' ')- stop it, or run '$0 down'"
  fi
  app_env
  : > "$LOG"
  # The http launch profile pins ASPNETCORE_ENVIRONMENT=Development over the shell's value.
  local profile=(--launch-profile http) mode=dev
  if [ "$PROD_BUNDLE" = 1 ]; then profile=(--no-launch-profile); mode=prod-bundle; fi
  # `( A && B & echo $! )` would parse as `{ A && B } &`, recording a forked bash rather
  # than the app - and that fork keeps this script's stdout open, hanging any pipeline.
  # exec replaces the subshell with dotnet, so $! really is the app.
  ( cd "$APP_DIR" && exec nohup dotnet run "${profile[@]}" >>"$LOG" 2>&1 ) &
  echo $! > "$PIDFILE"
  echo "$mode" > "$RUN_DIR/mode"
  printf '  building and seeding'
  for _ in $(seq 1 180); do
    if grep -q "Database Seeding Done" "$LOG" 2>/dev/null; then printf ' done\n'; return; fi
    if ! app_running; then printf '\n'; tail -20 "$LOG"; die "app exited during startup"; fi
    printf '.'; sleep 1
  done
  printf '\n'; die "app did not finish seeding - see $LOG"
}

seed_users() {
  local admin; admin=$(login "$ADMIN_USER" "$ADMIN_PASS")
  [ -n "$admin" ] || die "could not log in as $ADMIN_USER - the app may not have seeded"
  register "$DISABLED_USER"
  register "$ACTIVE_USER"
  local id; id=$(user_id "$admin" "$ACTIVE_USER")
  [ -n "$id" ] && api POST "/api/users/$id/activate" "$admin" >/dev/null
  info "test users ready"
}

summary() {
  local admin; admin=$(login "$ADMIN_USER" "$ADMIN_PASS")
  local games; games=$(api GET /api/games "$admin" | python3 -c '
import sys, json
try: g = json.load(sys.stdin)
except Exception: print("(unavailable)"); raise SystemExit
o = [x for x in g if x.get("IsOpen")]
print(", ".join(str(x["GameId"]) for x in o) or "(none)")')
  echo
  bold "Mundialito is running"
  info "url            $BASE"
  info "mode           $(cat "$RUN_DIR/mode" 2>/dev/null || echo dev)"
  info "swagger        $BASE/swagger"
  info "admin          $ADMIN_USER / $ADMIN_PASS"
  info "active user    $ACTIVE_USER / $TEST_PASS"
  info "disabled user  $DISABLED_USER / $TEST_PASS   (expect 403 when betting)"
  info "open games     $games"
  info "logs           $0 logs"
  echo
}

# A --prod-bundle instance is only trusted once it has shown two things: the backend Sentry SDK
# is off, and the page really serves the minified bundles. Either failing stops the app.
check_prod_bundle() {
  if grep -q "Initializing Hub for Dsn" "$LOG" || ! grep -q "Sentry SDK will be disabled" "$LOG"; then
    stop_app
    die "the backend Sentry SDK did not report itself disabled - refusing to run a Production-tagged instance (see $LOG)"
  fi
  local page lib app f
  page=$(curl -s "$BASE/")
  lib=$(printf '%s' "$page" | grep -o 'lib/lib-min-[a-f0-9]*\.js' | head -1 || true)
  app=$(printf '%s' "$page" | grep -o 'js/app-min-[a-f0-9]*\.js' | head -1 || true)
  if [ -z "$lib" ] || [ -z "$app" ]; then stop_app; die "the page does not reference the minified bundles"; fi
  for f in "$lib" "$app"; do
    if [ "$(curl -s -o /dev/null -w '%{http_code}' "$BASE/$f")" != 200 ]; then stop_app; die "$f does not load"; fi
  done
  info "serving $lib and $app, backend Sentry disabled"
}

cmd_up() {
  need docker; need dotnet; need python3; mkdir -p "$RUN_DIR"
  start_db; start_app
  [ "$PROD_BUNDLE" = 1 ] && check_prod_bundle
  seed_users; summary
}
cmd_logs()  { tail -f "$LOG"; }

cmd_client() {
  ( cd "$APP_DIR" && npx gulp )
  # gulp.dest restores the source mtime on the rewritten Index.cshtml, so MSBuild would keep
  # the old precompiled view and the app would request a bundle gulp has just deleted.
  touch "$APP_DIR/Views/Home/Index.cshtml"
  echo; info "remember to commit wwwroot/ and Views/Home/Index.cshtml together"
}

stop_app() {
  if app_running; then kill "$(cat "$PIDFILE")" 2>/dev/null || true; fi
  rm -f "$PIDFILE" "$RUN_DIR/mode"
  # `dotnet run` does not always take its child app down with it, so make sure the port is
  # actually free. Only ever touches the port this script started the app on.
  for _ in $(seq 1 10); do lsof -ti ":$PORT" >/dev/null 2>&1 || break; sleep 1; done
  if lsof -ti ":$PORT" >/dev/null 2>&1; then lsof -ti ":$PORT" | xargs kill -9 2>/dev/null || true; fi
  info "app stopped"
}

cmd_down() {
  stop_app
  docker compose -f "$ROOT/compose.yml" down >/dev/null 2>&1 || true
  info "database stopped"
}

# The database container has no volume, so `down` wipes it. restart swaps the app - between
# dev and --prod-bundle, or onto a fresh gulp build - and keeps the data it is testing against.
cmd_restart() {
  need dotnet; need python3; mkdir -p "$RUN_DIR"
  db_running || die "database is not running - try '$0 up'"
  stop_app; start_app
  [ "$PROD_BUNDLE" = 1 ] && check_prod_bundle
  summary
}

cmd_smoke() {
  need python3
  app_running || die "app is not running - try '$0 up'"
  python3 "$ROOT/scripts/smoke.py" --base "$BASE" "$@"
}

cmd_reset() {
  cmd_down
  # Seeding is gated on Teams.Count() == 0, so a wipe is the only way to re-seed - which is
  # also how you refresh LocalDev's relative fixtures once they have aged out.
  cmd_up
}

cmd_status() {
  db_running  && info "database  running" || info "database  stopped"
  if app_running; then
    info "app       running (pid $(cat "$PIDFILE")) $BASE, $(cat "$RUN_DIR/mode" 2>/dev/null || echo dev) bundles"
  else
    info "app       stopped"
  fi
}

# Regression checks for the stale-role bug (a role change must take effect on the token the
# user already holds - see Auth/Authorization/CurrentRoleHandler) and for the bet write path
# (PUT /api/games/{id}/mybet - see #170, #171). The bet cases assert the stored row, not just
# the status code: the defect they guard returned 400 *and* saved.
cmd_verify() {
  mkdir -p "$RUN_DIR"
  app_running || die "app is not running - try '$0 up'"
  local admin id t code fails=0
  bet_body()  { echo "{\"HomeScore\":${1:-1},\"AwayScore\":${2:-0},\"CardsMark\":\"1\",\"CornersMark\":\"2\"}"; }
  mybet()     { echo "/api/games/$1/mybet"; }
  body_field() { python3 -c "import sys,json
try: print(json.load(sys.stdin).get('$1',''))
except Exception: print('')" < "$RUN_DIR/body"; }
  check() { # check <label> <expected> <actual>
    if [ "$3" = "$2" ]; then info "PASS  $1 ($3)"; else info "FAIL  $1 got $3, expected $2"; fails=1; fi
  }
  db_query() { docker exec "$CONTAINER" psql -U mundialito -d mundialito -tAqc "$1"; }
  admin=$(login "$ADMIN_USER" "$ADMIN_PASS")
  local u="verify$(date +%s)"
  register "$u"
  t=$(login "$u" "$TEST_PASS")
  [ -n "$t" ] || die "could not log in as the freshly registered $u"
  id=$(user_id "$admin" "$u")
  local g1 g2
  read -r g1 g2 <<<"$(api GET /api/games "$admin" | python3 -c '
import sys, json
o = [x["GameId"] for x in json.load(sys.stdin) if x.get("IsOpen")]
print(o[0] if o else "", o[1] if len(o) > 1 else "")')"
  [ -n "$g1" ] && [ -n "$g2" ] || die "need two games open for betting - try '$0 reset'"

  bold "Role changes must apply to an already-issued token"

  code=$(api_code PUT "$(mybet "$g1")" "$t" "$(bet_body)")
  check "disabled user is refused" 403 "$code"

  api POST "/api/users/$id/activate" "$admin" >/dev/null
  code=$(api_code PUT "$(mybet "$g1")" "$t" "$(bet_body)")
  check "activation applies to the old token" 201 "$code"

  api DELETE "/api/users/$id/activate" "$admin" >/dev/null
  code=$(api_code PUT "$(mybet "$g2")" "$t" "$(bet_body)")
  check "deactivation applies to the old token" 403 "$code"

  echo
  bold "A bet has one address and one write verb"
  api POST "/api/users/$id/activate" "$admin" >/dev/null

  # The activation check above created the bet. Saving again must update it in place - a
  # double tap on Save is now two identical idempotent writes, not a POST then a PUT.
  local first second
  code=$(api_code PUT "$(mybet "$g1")" "$t" "$(bet_body 2 2)"); first=$(body_field BetId)
  check "a second save updates rather than creates" 200 "$code"
  code=$(api_code PUT "$(mybet "$g1")" "$t" "$(bet_body 3 3)"); second=$(body_field BetId)
  check "a third save updates rather than creates" 200 "$code"
  if [ -n "$first" ] && [ "$first" = "$second" ]; then
    info "PASS  every save addressed the same bet ($first)"
  else
    info "FAIL  saves produced bets '$first' and '$second'"; fails=1
  fi

  # The response must be sendable straight back. Echoing a create response used to 400 with
  # "Object reference not set to an instance of an object" because it carried no flat GameId.
  api GET "$(mybet "$g1")" "$t" > "$RUN_DIR/mybet.json"
  code=$(api_code PUT "$(mybet "$g1")" "$t" "$(cat "$RUN_DIR/mybet.json")")
  check "the server's own response round-trips" 200 "$code"

  # [Required] on a non-nullable int was a no-op: this body used to record a silent 0-0 bet.
  code=$(api_code PUT "$(mybet "$g1")" "$t" '{"AwayScore":1,"CardsMark":"1","CornersMark":"2"}')
  check "an omitted score is refused" 400 "$code"

  code=$(api_code GET "$(mybet 999999)" "$t")
  check "GET on an unknown game is not fabricated" 404 "$code"
  code=$(api_code PUT "$(mybet 999999)" "$t" "$(bet_body)")
  check "PUT on an unknown game is refused" 404 "$code"

  # The old fork is gone, deliberately rather than aliased.
  code=$(api_code POST /api/bets "$t" "$(bet_body)")
  check "POST /api/bets no longer exists" 404 "$code"
  code=$(api_code PUT "/api/bets/$first" "$t" "$(bet_body)")
  check "PUT /api/bets/{id} no longer exists" 404 "$code"

  # The defect this whole change exists for. Asserting the *stored row* is the point: the
  # status code alone never showed it, because the rejection returned 400 and saved anyway.
  # It needs a bet that already exists on a game whose deadline has passed, so bet on the
  # second open game while it is open and push its kickoff back afterwards - reading a
  # game the seed already closed would compare two "no bet" placeholders and pass for free.
  if ! db_running; then
    info "SKIP  database container not running, cannot assert the stored row"
  else
    local betid row_before row_after audit_before audit_after
    local cols='"HomeScore","AwayScore","CardsMark","CornersMark","GameId"'
    # ActionType ordinals: 0 CREATE, 1 UPDATE, 2 DELETE, 3 ERROR, 4 UNAUTHORIZED_ACCESS.
    local audit_rows='SELECT count(*) FROM "ActionLogs" WHERE "ObjectType" = '"'"'Bet'"'"' AND "Type" = 3;'
    code=$(api_code PUT "$(mybet "$g2")" "$t" "$(bet_body 1 2)"); betid=$(body_field BetId)
    check "a bet can be placed while the game is open" 201 "$code"
    db_query "UPDATE \"Games\" SET \"Date\" = \"Date\" - interval '30 days' WHERE \"GameId\" = $g2;" >/dev/null
    row_before=$(db_query "SELECT $cols FROM \"Bets\" WHERE \"BetId\" = $betid;")
    audit_before=$(db_query "$audit_rows")
    code=$(api_code PUT "$(mybet "$g2")" "$t" "$(bet_body 9 9)")
    check "a past-deadline save is refused" 409 "$code"
    row_after=$(db_query "SELECT $cols FROM \"Bets\" WHERE \"BetId\" = $betid;")
    audit_after=$(db_query "$audit_rows")
    if [ -n "$row_before" ] && [ "$row_before" = "$row_after" ]; then
      info "PASS  the refused save left the stored row untouched ($row_after)"
    else
      info "FAIL  the refused save changed the row: '$row_before' -> '$row_after'"; fails=1
    fi
    # The audit row must land even though the write it describes was refused - it is written
    # through a context of its own, so it neither depends on nor commits the business change.
    if [ "$audit_after" -gt "$audit_before" ] 2>/dev/null; then
      info "PASS  the refusal was recorded in ActionLogs ($audit_before -> $audit_after)"
    else
      info "FAIL  no ActionLogs row for the refusal ($audit_before -> $audit_after)"; fails=1
    fi
    # Put the fixture back so a second run still finds two open games.
    db_query "UPDATE \"Games\" SET \"Date\" = \"Date\" + interval '30 days' WHERE \"GameId\" = $g2;" >/dev/null
  fi

  echo
  bold "Admin writes: a bad id is a 404, and a simulation any player can run stays in memory"

  code=$(api_code PUT /api/teams/999999 "$admin" '{"Name":"X","ShortName":"XXX","Flag":"f.png","Logo":"l.png"}')
  check "PUT an unknown team is refused" 404 "$code"
  code=$(api_code PUT /api/stadiums/999999 "$admin" '{"Name":"X","City":"Y","Capacity":100}')
  check "PUT an unknown stadium is refused" 404 "$code"

  # A game past kickoff with no result entered - the only state SimulateGame accepts.
  local gpending
  gpending=$(api GET /api/games "$admin" | python3 -c '
import sys, json
p = [x["GameId"] for x in json.load(sys.stdin) if x.get("IsPendingUpdate")]
print(p[0] if p else "")')
  if [ -z "$gpending" ]; then
    info "SKIP  no game is pending a result, cannot check the simulation"
  elif ! db_running; then
    info "SKIP  database container not running, cannot check the simulation"
  else
    # Simulating is a player-facing what-if, not an admin write: the client offers the panel
    # to every signed-in user, and gating it AdminOnly 403'd that button for every player.
    # An unapproved account is still out, the same as every other thing a player can do.
    api DELETE "/api/users/$id/activate" "$admin" >/dev/null
    code=$(api_code POST "/api/games/$gpending/simulate" "$t" '{"HomeScore":2,"AwayScore":1,"CardsMark":"1","CornersMark":"1"}')
    check "a disabled account cannot simulate" 403 "$code"
    api POST "/api/users/$id/activate" "$admin" >/dev/null
    code=$(api_code POST "/api/games/$gpending/simulate" "$t" '{"HomeScore":2,"AwayScore":1,"CardsMark":"1","CornersMark":"1"}')
    check "an active player can simulate" 200 "$code"

    # Betting on it closed days ago, so seed the bet directly - it is a fixture, not a case.
    # An exact-score match, so the simulated table must show this user with points.
    db_query "INSERT INTO \"Bets\" (\"UserId\",\"GameId\",\"HomeScore\",\"AwayScore\",\"CardsMark\",\"CornersMark\",\"CornersWin\",\"GameMarkWin\",\"ResultWin\",\"CardsWin\",\"MaxPoints\") VALUES ('$id', $gpending, 2, 1, '1', '1', false, false, false, false, false) ON CONFLICT DO NOTHING;" >/dev/null

    local game_before bet_before game_after bet_after points
    game_before=$(db_query "SELECT coalesce(\"HomeScore\"::text,'-'), coalesce(\"AwayScore\"::text,'-') FROM \"Games\" WHERE \"GameId\" = $gpending;")
    bet_before=$(db_query "SELECT coalesce(\"Points\"::text,'-'), \"GameMarkWin\" FROM \"Bets\" WHERE \"UserId\" = '$id' AND \"GameId\" = $gpending;")

    code=$(api_code POST "/api/games/$gpending/simulate" "$admin" '{"HomeScore":2,"AwayScore":1,"CardsMark":"1","CornersMark":"1"}')
    check "an admin can simulate a pending game" 200 "$code"
    points=$(python3 -c "
import sys, json
try: t = json.load(open('$RUN_DIR/body'))
except Exception: print('-'); raise SystemExit
print(next((u.get('Points') for u in t if u.get('Username') == '$u'), '-'))")
    if [ "$points" != "-" ] && [ "${points:-0}" -gt 0 ] 2>/dev/null; then
      info "PASS  the simulated result scored the bet ($points points)"
    else
      info "FAIL  the simulated table gave $u '$points' points"; fails=1
    fi

    game_after=$(db_query "SELECT coalesce(\"HomeScore\"::text,'-'), coalesce(\"AwayScore\"::text,'-') FROM \"Games\" WHERE \"GameId\" = $gpending;")
    bet_after=$(db_query "SELECT coalesce(\"Points\"::text,'-'), \"GameMarkWin\" FROM \"Bets\" WHERE \"UserId\" = '$id' AND \"GameId\" = $gpending;")
    if [ "$game_before" = "$game_after" ] && [ "$bet_before" = "$bet_after" ]; then
      info "PASS  the simulation reached no row (game $game_after, bet $bet_after)"
    else
      info "FAIL  the simulation was persisted: game '$game_before'->'$game_after', bet '$bet_before'->'$bet_after'"; fails=1
    fi
  fi

  echo
  bold "Every action that resolves its caller still resolves it"

  # Eleven actions across five controllers used to each run their own
  # FindByNameAsync(Identity.Name); they share one ICurrentUser now. These are the ones
  # scripts above do not already touch - a caller that fails to resolve answers 401, so a
  # 200 here is the whole assertion.
  check "GET /api/users/me" 200 "$(api_code GET /api/users/me "$t")"
  check "GET /api/users/me/progress" 200 "$(api_code GET /api/users/me/progress "$t")"
  check "GET /api/account/UserInfo" 200 "$(api_code GET /api/account/UserInfo "$t")"
  check "GET /api/stats/me" 200 "$(api_code GET /api/stats/me "$t")"
  check "follow another user" 200 "$(api_code POST "/api/users/follow/$ACTIVE_USER" "$t")"
  check "unfollow them again" 200 "$(api_code DELETE "/api/users/follow/$ACTIVE_USER" "$t")"

  bold "An unmatched api/ route is a reportable 404, not a refusal"

  # The catch-all in Program.cs exists so a missing endpoint stops answering 200 with
  # index.html. It has to answer in a shape the client will report: ErrorHandler.js treats
  # any 4xx carrying a Message as a rule the API meant to enforce and keeps it out of
  # Sentry, which would have hidden exactly the calls this route is here to catch.
  # Not /api/account/logout on purpose - if a real logout endpoint is ever added, a check
  # pinned to that path would fail and read as "the catch-all broke".
  check "an unknown api route is a 404" 404 "$(api_code POST /api/definitely-not-an-endpoint "$t")"
  check "an unknown api route is not disguised as a refusal" "" "$(body_field Message)"
  check "an unknown api route still says what was missing" "No API endpoint at 'api/definitely-not-an-endpoint'" "$(body_field detail)"

  echo
  [ "$fails" = "0" ] && bold "all checks passed" || { bold "checks FAILED"; return 1; }
}

cmd="${1:-up}"
[ $# -gt 0 ] && shift
case "$cmd" in
  up)      parse_mode "$@"; cmd_up ;;
  restart) parse_mode "$@"; cmd_restart ;;
  down)    cmd_down ;;
  reset)   cmd_reset ;;
  status)  cmd_status ;;
  logs)    cmd_logs ;;
  verify)  cmd_verify ;;
  client)  cmd_client ;;
  smoke)   cmd_smoke "$@" ;;
  *)       sed -n '2,17p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 1 ;;
esac
