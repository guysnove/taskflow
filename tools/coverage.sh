#!/usr/bin/env bash
# Course 2, Chapter 17: which lines of the BUSINESS LOGIC (Domain + Application) do the UNIT tests run?
# A guide to find untested rules — not a score to chase.
# Course 2, Chapter 19: also a QUALITY GATE. CI runs this script, and it fails the build if coverage
# DROPS below the floor (a little under today's numbers). The floor stops slow erosion; it isn't a target.
MIN_LINE=${MIN_LINE:-80}
MIN_BRANCH=${MIN_BRANCH:-90}
set -e
cd "$(dirname "$0")/.."
rm -rf TestResults/coverage
dotnet test TaskFlow.Tests --filter "FullyQualifiedName~TaskFlow.Tests.Unit" \
    --collect "XPlat Code Coverage" --results-directory TestResults/coverage | grep -E "Passed!|Failed!"
dotnet tool restore > /dev/null
dotnet reportgenerator -reports:"TestResults/coverage/*/coverage.cobertura.xml" -targetdir:TestResults/coverage/report \
    -reporttypes:"TextSummary;Html" -assemblyfilters:"+TaskFlow.Domain;+TaskFlow.Application" > /dev/null
cat TestResults/coverage/report/Summary.txt
echo "Details, line by line: TestResults/coverage/report/index.html"

line=$(grep -m1 "Line coverage:" TestResults/coverage/report/Summary.txt | grep -oE "[0-9.]+")
branch=$(grep -m1 "Branch coverage:" TestResults/coverage/report/Summary.txt | grep -oE "[0-9.]+" | head -n 1)
echo "Quality gate: lines ${line}% (floor ${MIN_LINE}%), branches ${branch}% (floor ${MIN_BRANCH}%)"
if awk -v l="$line" -v b="$branch" -v ml="$MIN_LINE" -v mb="$MIN_BRANCH" 'BEGIN { exit !(l < ml || b < mb) }'; then
    echo "FAILED: unit-test coverage of the business logic fell below the floor."
    exit 1
fi
echo "Passed."
