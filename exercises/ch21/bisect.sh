#!/usr/bin/env bash
# Chapter 21: the test "git bisect run" runs at every step.
# Exit code 0 = this commit is good, 1 = bad, 125 = can't test this commit (skip it).
# The new test didn't exist in older commits, so we copy it in, run only it, and take it out again.
here="$(cd "$(dirname "$0")" && pwd)"
cp "$here/steps/bisect/RoundingTests.cs" Ch21.Invoices.Tests/RoundingTests.cs
dotnet build Ch21.slnx -v q -nologo > /dev/null 2>&1 || { rm Ch21.Invoices.Tests/RoundingTests.cs; exit 125; }
dotnet test Ch21.slnx --no-build --filter RoundingTests > /dev/null 2>&1; result=$?
rm Ch21.Invoices.Tests/RoundingTests.cs
exit $result
