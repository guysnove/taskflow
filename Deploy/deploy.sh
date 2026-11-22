#!/usr/bin/env bash
# Chapter 24: deploy a published TaskFlow to a "server" folder, check it, and roll back automatically if it's unhealthy.
#   releases/<version>/   each published build, kept       current -> releases/<version>   the live one (a symlink)
# Usage: deploy.sh <server-folder> <published-folder> <version> [extra environment, e.g. ConnectionStrings__TaskFlow=…]
set -uo pipefail
SERVER=${1:?server folder}; BUILD=${2:?published folder}; VERSION=${3:?version}; shift 3
PORT=${PORT:-5040}; LOG="$SERVER/logs"; mkdir -p "$SERVER/releases" "$LOG" "$SERVER/shared"
say() { echo "$(date -u +%H:%M:%S)  $*"; }

stop() {    # stop the running version, and wait until it has really gone (a systemd stop, on a real server)
  [ -f "$SERVER/app.pid" ] || return 0
  local pid; pid=$(cat "$SERVER/app.pid"); kill "$pid" 2>/dev/null
  for _ in $(seq 30); do kill -0 "$pid" 2>/dev/null || break; sleep 0.5; done
  rm -f "$SERVER/app.pid"
}
start() {   # start whatever "current" points at (a systemd restart, on a real server)
  local log; log="$LOG/$(basename "$(readlink -f "$SERVER/current")").log"
  ( cd "$SERVER/current" && exec env ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS="http://127.0.0.1:$PORT" \
      ConnectionStrings__TaskFlow="Data Source=$SERVER/shared/taskflow.db" Email__PickupDirectory="$SERVER/shared/outbox" "$@" \
      dotnet TaskFlow.dll >> "$log" 2>&1 ) &
  echo $! > "$SERVER/app.pid"; disown
}
healthy() { for _ in $(seq 20); do [ "$(curl -s -m 2 "http://127.0.0.1:$PORT/health")" = "Healthy" ] && return 0; sleep 1; done; return 1; }

previous=""; [ -L "$SERVER/current" ] && previous=$(basename "$(readlink -f "$SERVER/current")")
say "deploying $VERSION (live now: ${previous:-nothing})"
rm -rf "$SERVER/releases/$VERSION"; cp -r "$BUILD" "$SERVER/releases/$VERSION"
stop; ln -sfn "releases/$VERSION" "$SERVER/current"; start "$@"
if healthy; then say "$VERSION is healthy: deployed"; exit 0; fi
say "$VERSION is NOT healthy after 20 s: rolling back"
grep -m 1 -E "Unhandled exception|Exception:|crit:" "$LOG/$VERSION.log" | cut -c1-150 | sed 's/^/          /'
stop
if [ -n "$previous" ]; then
  ln -sfn "releases/$previous" "$SERVER/current"; start
  healthy && say "rolled back to $previous: healthy" || say "rollback FAILED: call a human"
else say "nothing to roll back to"; fi
exit 1
