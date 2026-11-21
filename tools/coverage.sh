#!/usr/bin/env bash
# Course 2, Chapter 17: which lines of the BUSINESS LOGIC (Domain + Application) do the UNIT tests run?
# A guide to find untested rules — not a score to chase.
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
