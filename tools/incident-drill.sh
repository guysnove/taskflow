#!/usr/bin/env bash
# Chapter 23: an incident DRILL (a "game day"), run for real on this machine, to practise writing an incident report.
# A deploy changes the database setting to a folder that doesn't exist. Everything printed is real; times are UTC.
set -uo pipefail
# Usage (after dotnet build): bash tools/incident-drill.sh .
TF=${1:?repo}; cd "$TF/TaskFlow"; L=/tmp/ch23-drill; rm -rf $L; mkdir -p $L
now() { date -u +%H:%M:%S; }
start() { ASPNETCORE_ENVIRONMENT=Production Email__PickupDirectory=$L/outbox ConnectionStrings__TaskFlow="$1" \
          dotnet run --no-build --no-launch-profile --urls http://127.0.0.1:5025 > "$2" 2>&1 & echo $!; }
probe() { echo "$(now)  GET /health → $(curl -s -m 3 -o /dev/null -w '%{http_code}' http://127.0.0.1:5025/health || true)"; }
echo "$(now)  version 1 running (database: $L/taskflow.db)"
P=$(start "Data Source=$L/taskflow.db" $L/v1.log); sleep 12; probe
echo "$(now)  DEPLOY: new settings, database 'Data Source=/var/lib/taskflow/data/taskflow.db' (that folder doesn't exist here)"
kill $P; sleep 2
P=$(start "Data Source=/var/lib/taskflow/data/taskflow.db" $L/v2.log); sleep 12; probe; sleep 5; probe
echo "$(now)  process still running? $(kill -0 $P 2>/dev/null && echo yes || echo no)"
echo "$(now)  the log says:"; grep -m 3 -oE '"(Message|Exception)":"[^"]{0,160}' $L/v2.log | sed 's/^/        /'; grep -m 2 -E "fail|crit|Unhandled|SqliteException" $L/v2.log | cut -c1-200 | sed 's/^/        /'
echo "$(now)  MITIGATE: roll back to the previous settings and restart"
kill $P 2>/dev/null; sleep 2
P=$(start "Data Source=$L/taskflow.db" $L/v3.log); sleep 12; probe
kill $P 2>/dev/null; sleep 1
echo "$(now)  drill over"
