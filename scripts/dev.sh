#!/usr/bin/env bash
#
# Local development environment for Mundialito.
#
#   ./scripts/dev.sh up      start database + app, seed test users, print a summary
#   ./scripts/dev.sh down    stop the app and the database
#   ./scripts/dev.sh reset   wipe the database and start again from a fresh seed
#   ./scripts/dev.sh status  what is running
#   ./scripts/dev.sh logs    follow the app log
#   ./scripts/dev.sh verify  check that a role change takes effect without re-login
#   ./scripts/dev.sh client  rebuild the AngularJS bundles with gulp
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

# The tournament window drives registration and the general-bet deadline. Relative to now so
# it never goes stale, unlike the dates committed in appsettings.Development.json.
app_env() {
  ASPNETCORE_ENVIRONMENT=Development
  App__TournamentDBCreatorName=LocalDev
  App__TournamentStartDate="$(date -u -v+1d '+%d/%m/%Y %H:%M' 2>/dev/null || date -u -d '+1 day' '+%d/%m/%Y %H:%M')"
  App__TournamentEndDate="$(date -u -v+180d '+%d/%m/%Y %H:%M' 2>/dev/null || date -u -d '+180 days' '+%d/%m/%Y %H:%M')"
  export ASPNETCORE_ENVIRONMENT App__TournamentDBCreatorName App__TournamentStartDate App__TournamentEndDate
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
  # `( A && B & echo $! )` would parse as `{ A && B } &`, recording a forked bash rather
  # than the app - and that fork keeps this script's stdout open, hanging any pipeline.
  # exec replaces the subshell with dotnet, so $! really is the app.
  ( cd "$APP_DIR" && exec nohup dotnet run --launch-profile http >>"$LOG" 2>&1 ) &
  echo $! > "$PIDFILE"
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
  info "swagger        $BASE/swagger"
  info "admin          $ADMIN_USER / $ADMIN_PASS"
  info "active user    $ACTIVE_USER / $TEST_PASS"
  info "disabled user  $DISABLED_USER / $TEST_PASS   (expect 403 when betting)"
  info "open games     $games"
  info "logs           $0 logs"
  echo
}

cmd_up()    { need docker; need dotnet; need python3; mkdir -p "$RUN_DIR"; start_db; start_app; seed_users; summary; }
cmd_logs()  { tail -f "$LOG"; }
cmd_client(){ ( cd "$APP_DIR" && npx gulp ); echo; info "remember to commit wwwroot/ and Views/Home/Index.cshtml together"; }

cmd_down() {
  if app_running; then kill "$(cat "$PIDFILE")" 2>/dev/null || true; fi
  rm -f "$PIDFILE"
  # `dotnet run` does not always take its child app down with it, so make sure the port is
  # actually free. Only ever touches the port this script started the app on.
  for _ in $(seq 1 10); do lsof -ti ":$PORT" >/dev/null 2>&1 || break; sleep 1; done
  if lsof -ti ":$PORT" >/dev/null 2>&1; then lsof -ti ":$PORT" | xargs kill -9 2>/dev/null || true; fi
  info "app stopped"
  docker compose -f "$ROOT/compose.yml" down >/dev/null 2>&1 || true
  info "database stopped"
}

cmd_reset() {
  cmd_down
  # Seeding is gated on Teams.Count() == 0, so a wipe is the only way to re-seed - which is
  # also how you refresh LocalDev's relative fixtures once they have aged out.
  cmd_up
}

cmd_status() {
  db_running  && info "database  running" || info "database  stopped"
  if app_running; then info "app       running (pid $(cat "$PIDFILE")) $BASE"; else info "app       stopped"; fi
}

# Regression check for the stale-role bug: a role change must take effect on the token the
# user already holds. See Auth/Authorization/CurrentRoleHandler.
cmd_verify() {
  mkdir -p "$RUN_DIR"
  app_running || die "app is not running - try '$0 up'"
  local admin id t code fails=0
  bet_on() { echo "{\"GameId\":$1,\"HomeScore\":1,\"AwayScore\":0,\"CardsMark\":\"1\",\"CornersMark\":\"2\"}"; }
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
print(o[0], o[1] if len(o) > 1 else "")')"
  [ -n "$g1" ] && [ -n "$g2" ] || die "need two games open for betting - try '$0 reset'"

  bold "Role changes must apply to an already-issued token"

  code=$(api_code POST /api/bets "$t" "$(bet_on "$g1")")
  [ "$code" = "403" ] && info "PASS  disabled user is refused ($code)" || { info "FAIL  disabled user got $code, expected 403"; fails=1; }

  api POST "/api/users/$id/activate" "$admin" >/dev/null
  code=$(api_code POST /api/bets "$t" "$(bet_on "$g1")")
  [ "$code" = "200" ] && info "PASS  activation applies to the old token ($code)" || { info "FAIL  after activation got $code, expected 200"; fails=1; }

  api DELETE "/api/users/$id/activate" "$admin" >/dev/null
  code=$(api_code POST /api/bets "$t" "$(bet_on "$g2")")
  [ "$code" = "403" ] && info "PASS  deactivation applies to the old token ($code)" || { info "FAIL  after deactivation got $code, expected 403"; fails=1; }

  echo
  [ "$fails" = "0" ] && bold "all checks passed" || { bold "checks FAILED"; return 1; }
}

case "${1:-up}" in
  up)     cmd_up ;;
  down)   cmd_down ;;
  reset)  cmd_reset ;;
  status) cmd_status ;;
  logs)   cmd_logs ;;
  verify) cmd_verify ;;
  client) cmd_client ;;
  *)      sed -n '2,12p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 1 ;;
esac
