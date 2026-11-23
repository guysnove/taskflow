#!/usr/bin/env bash
# Course 2, Chapter 30: the AUTOMATED half of an engineering review — every check that a machine can do, in one run,
# with one line of evidence each. The other half (reading the design, the risks, the docs) is for people:
# docs/review/2026-11-23-taskflow-2.0.md. Usage: bash tools/review.sh   (from the repository root; about 3 minutes)
set -uo pipefail
cd "$(dirname "$0")/.."
ok=0; bad=0
line() { if [ "$2" = yes ]; then ok=$((ok + 1)); printf '✓ %-28s %s\n' "$1" "$3"; else bad=$((bad + 1)); printf '✗ %-28s %s\n' "$1" "$3"; fi; }

out=$(dotnet build -warnaserror -nologo -v q 2>&1); w=$(echo "$out" | grep -oE "[0-9]+ Warning" | head -1)
line "build, warnings as errors" "$(echo "$out" | grep -q "Build succeeded" && echo yes || echo no)" "${w:-?}(s), $(echo "$out" | grep -oE "[0-9]+ Error" | head -1)(s)"
dotnet format --verify-no-changes --no-restore > /dev/null 2>&1 && f=yes || f=no
line "format" "$f" "$( [ $f = yes ] && echo "no changes needed" || echo "run dotnet format")"
t=$(dotnet test --no-build --filter "Category!=Growth&Category!=E2E" 2>&1 | grep -E "Passed!|Failed!" | grep -oE "Failed: +[0-9]+, Passed: +[0-9]+" | tr -s ' ')
line "tests (unit + integration)" "$(echo "$t" | grep -q "Failed: 0" && echo yes || echo no)" "$t"
e=$(dotnet test --no-build --filter "Category=E2E" 2>&1 | grep -E "Passed!|Failed!" | grep -oE "Failed: +[0-9]+, Passed: +[0-9]+" | tr -s ' ')
line "tests (end to end, browser)" "$(echo "$e" | grep -q "Failed: 0" && echo yes || echo no)" "${e:-not run}"
cov=$(bash tools/coverage.sh 2>&1); lc=$(echo "$cov" | grep -m1 "Line coverage:" | grep -oE "[0-9.]+%"); bc=$(echo "$cov" | grep -m1 "Branch coverage:" | grep -oE "[0-9.]+%" | head -1)
line "coverage gate (Domain+App)" "$(echo "$cov" | grep -qi "below" && echo no || echo yes)" "lines $lc, branches $bc"
tr=$(dotnet run --project tools/Traceability 2>&1 | grep -E "requirements in scope have")
line "requirements traced to tests" "yes" "$tr"
dc=$(dotnet run --project tools/DocsCheck -- . 2>&1 | tail -1)
line "documentation links" "$(echo "$dc" | grep -q "OK" && echo yes || echo no)" "$dc"
v=$(dotnet list package --vulnerable --include-transitive 2>&1 | grep -c "has the following vulnerable packages")
line "known vulnerable packages" "$(is() { [ "$1" = 0 ] && echo yes || echo no; }; is "$v")" "$v project(s) affected"
cx=$(bash tools/metrics/complexity.sh 10 2>/dev/null | wc -l)
line "methods with complexity > 10" "$( [ "$cx" -le 3 ] && echo yes || echo no)" "$cx (Chapter 9's rule of thumb: few, and each explained)"
debt=$(grep -cE "^\| TD-[0-9]+ " docs/tech-debt.md); paid=$(grep -cE "^\| ~~TD-[0-9]+~~" docs/tech-debt.md)
line "technical debt register" "yes" "$debt open, $paid paid (docs/tech-debt.md)"
adr=$(ls docs/adr/0*.md | wc -l); line "architecture decisions" "yes" "$adr ADRs (docs/adr/)"
echo "review: $ok checks passed, $bad need attention"
[ "$bad" -eq 0 ]
