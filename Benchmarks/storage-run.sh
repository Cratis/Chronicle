#!/bin/bash
# Runs the storage-provider benchmark harness (Chronicle.Benchmarks.Storage).
#
# Usage: storage-run.sh [--profile smoke|small|medium] [--providers sqlite,pg,mssql,mongo]
#                       [--repetitions N] [--max-load X] [--output DIR] [extra harness flags]
#
# Exit codes: 0 ok, 2 medium profile not confirmed, 3 machine too loaded (nothing measured).
# The medium profile (1M events) needs --yes-i-know-this-is-long.
set -u

here="$(cd "$(dirname "$0")" && pwd)"
project="$here/Chronicle.Benchmarks.Storage/Chronicle.Benchmarks.Storage.csproj"
repetitions=1
passthrough=()

while [ $# -gt 0 ]; do
    case "$1" in
        --repetitions) repetitions="$2"; shift 2 ;;
        *) passthrough+=("$1"); shift ;;
    esac
done

echo "Building storage benchmarks..."
dotnet build "$project" -c Release /p:DisableDockerBuild=true /p:CratisProxiesOutputPath= || exit $?

rep=1
while [ "$rep" -le "$repetitions" ]; do
    echo "Repetition $rep of $repetitions"
    dotnet run --project "$project" -c Release --no-build -- "${passthrough[@]}" --repetition "$rep"
    code=$?
    if [ "$code" -ne 0 ]; then
        # 2 = confirmation missing, 3 = machine load too high: stop, never retry into a loaded machine.
        exit "$code"
    fi
    rep=$((rep + 1))
done
