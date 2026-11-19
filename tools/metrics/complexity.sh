#!/usr/bin/env bash
# Course 2, Chapter 9: list TaskFlow's methods whose cyclomatic complexity is above a threshold.
# Usage: tools/metrics/complexity.sh [threshold, default 5]
set -uo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
echo "CA1502: ${1:-5}" > "$here/CodeMetricsConfig.txt"
dotnet build "$here/../../TaskFlow/TaskFlow.csproj" --no-incremental -nologo \
    -p:CustomAfterMicrosoftCommonTargets="$here/metrics.targets" 2>&1 \
  | grep "warning CA1502" \
  | sed -E "s|.*/([A-Za-z]+)\.cs\([0-9,]+\): warning CA1502: '([^']*)' has a cyclomatic complexity of '([0-9]+)'.*|\3  \1.\2|" \
  | sort -u | sort -rn
