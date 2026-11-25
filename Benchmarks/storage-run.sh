#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Runs the storage-provider benchmark harness (Chronicle.Benchmarks.Storage).
#
# Usage: storage-run.sh [--profile smoke|short|medium] [--providers sqlite,pg,mssql,mongo]
#                       [--repetitions N] [--max-load X] [--output DIR] [extra harness flags]
#
# Exit codes: 0 ok, 1 a benchmark or measured operation failed, 64 bad usage, 2 medium profile not confirmed, 3 machine too loaded (nothing measured), 4 profile time budget exhausted (partial results kept).
# The medium profile (1M events) needs --yes-i-know-this-is-long.
set -u

here="$(cd "$(dirname "$0")" && pwd)"
project="$here/Chronicle.Benchmarks.Storage/Chronicle.Benchmarks.Storage.csproj"
repetitions=1
passthrough=()

while [ $# -gt 0 ]; do
    case "$1" in
        --repetitions)
            repetitions="${2:-}"
            case "$repetitions" in
                ""|*[!0-9]*) echo "--repetitions needs a positive integer." >&2; exit 64 ;;
            esac
            if [ "$((10#$repetitions))" -lt 1 ]; then
                echo "--repetitions needs a positive integer." >&2
                exit 64
            fi
            shift 2 ;;
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
