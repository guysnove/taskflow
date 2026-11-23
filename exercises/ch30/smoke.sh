#!/usr/bin/env bash
# Course 2, Chapter 30: a SMOKE TEST for a running TaskFlow — after every deploy, in CI and by hand.
# It does what a first user does: is it up, can I register, add a task, see it in the API, finish it?
# It creates one throwaway account (smoke-<time>@example.com). Exit code 0 = every check passed.
#   bash tools/smoke.sh http://127.0.0.1:5040
set -uo pipefail
BASE=${1:?base address, e.g. http://127.0.0.1:5040}; J=$(mktemp); trap 'rm -f "$J"' EXIT
pass=0; fail=0
check() {   # check <name> <ok?> <detail>
  if [ "$2" = yes ]; then pass=$((pass + 1)); printf 'PASS  %-34s %s\n' "$1" "$3"; else fail=$((fail + 1)); printf 'FAIL  %-34s %s\n' "$1" "$3"; fi
}
is() { [ "$1" = "$2" ] && echo yes || echo no; }
token() { curl -s -c "$J" -b "$J" "$BASE$1" | grep -o 'name="__RequestVerificationToken" type="hidden" value="[^"]*"' | head -1 | sed 's/.*value="//; s/"$//'; }

h=$(curl -s -m 5 "$BASE/health"); check "GET /health" "$(is "$h" Healthy)" "$h"
d=$(curl -s -m 5 "$BASE/health/details" | grep -o '"status": "[A-Za-z]*"' | head -1 | cut -d'"' -f4)
check "GET /health/details" "$( [ "$d" = Healthy ] || [ "$d" = Degraded ] && echo yes || echo no)" "$d"
c=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/account/signin"); check "GET /account/signin" "$(is "$c" 200)" "$c"

email="smoke-$(date +%s)@example.com"; t=$(token /account/register)
c=$(curl -s -o /dev/null -w '%{http_code}' -c "$J" -b "$J" -X POST "$BASE/account/register" --data-urlencode "Email=$email" \
    --data-urlencode "DisplayName=Smoke" --data-urlencode "Password=Smoke-test-2026!" --data-urlencode "ConfirmPassword=Smoke-test-2026!" \
    --data-urlencode "__RequestVerificationToken=$t")
check "register $email" "$(is "$c" 302)" "$c"

title="Smoke test $(date +%H%M%S)"; t=$(token /tasks/new)
c=$(curl -s -o /dev/null -w '%{http_code}' -c "$J" -b "$J" -X POST "$BASE/tasks/new" --data-urlencode "Title=$title" \
    --data-urlencode "Priority=Medium" --data-urlencode "__RequestVerificationToken=$t")
check "create a task (form)" "$(is "$c" 302)" "$c"

json=$(curl -s -b "$J" "$BASE/api/tasks")
id=$(echo "$json" | grep -o "\"id\":[0-9]*,\"title\":\"$title\"" | grep -o '[0-9]*' | head -1)
check "GET /api/tasks shows it" "$( [ -n "$id" ] && echo yes || echo no)" "id ${id:-none}"

t=$(token /tasks)
c=$(curl -s -o /dev/null -w '%{http_code}' -c "$J" -b "$J" -X POST "$BASE/tasks/${id:-0}/done" --data-urlencode "__RequestVerificationToken=$t")
done=$(curl -s -b "$J" "$BASE/api/tasks/${id:-0}" | grep -o '"done":[a-z]*' | cut -d: -f2)
check "complete it" "$(is "$done" true)" "HTTP $c, done: ${done:-?}"

c=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/api/tasks"); check "API without sign-in is refused" "$(is "$c" 401)" "$c"

echo "smoke test: $pass passed, $fail failed"
[ "$fail" -eq 0 ]
