#!/usr/bin/env bash
# Course 2, Chapter 21: tests for tools/hooks/commit-msg (run by CI). Each case: expected result, message.
hook="$(dirname "$0")/commit-msg"; file="$(mktemp)"; failures=0
check() {
  printf '%s\n' "$2" > "$file"
  if bash "$hook" "$file" 2>/dev/null; then got=ok; else got=rejected; fi
  if [ "$got" = "$1" ]; then echo "  pass  $1: $2"; else echo "  FAIL  expected $1, got $got: $2"; failures=$((failures + 1)); fi
}
check ok       "Add an overdue filter to the task list"
check ok       "Refuse discounts outside 0-100% (bug #4)"
check ok       "fixup! Add an overdue filter to the task list"
check ok       "Merge branch 'main' into feature/overdue"
check rejected ""
check rejected "wip"
check rejected "Fix"
check rejected "Add an overdue filter."
check rejected "add an overdue filter"
check rejected "Add an overdue filter to the task list so people can see what they missed last week"
rm -f "$file"
echo "$failures failure(s)"; [ "$failures" -eq 0 ]
