#!/bin/bash

# Runs an integration test project and, on failure, re-runs ONLY the failed
# tests up to a number of attempts. This absorbs transient flakiness in the
# Chronicle pipeline (a projection/reactor partition that occasionally does not
# materialize a read model within the polling deadline) without masking real
# regressions: a deterministically failing test fails on every attempt and the
# job still goes red.
#
# Usage:
#   run-tests-with-retry.sh <project> <test-filter> [extra dotnet test args...]
#
# <test-filter> is passed verbatim to `dotnet test --filter` for the first run,
# so it can be a bare namespace expression (FullyQualifiedName~Ns) or a sharded
# OR-expression (FullyQualifiedName~Ns.A.|FullyQualifiedName~Ns.B.).
#
# A rescued run is not a clean run. When a retry turns the job green, every test that
# failed on an earlier attempt is named in a ::warning:: annotation and appended to
# $RESULTS_DIR/flaky-tests.log, so the flake leaves a record instead of disappearing
# into a green check. CI already uploads $RESULTS_DIR as the coverage artifact, so the
# ledger travels with the run for free.
#
# Environment:
#   TEST_MAX_ATTEMPTS    Total attempts including the first run (default: 3).
#   TEST_MAX_RETRY_TESTS Maximum number of failed tests that will be retried. Above
#                        this the failure is treated as systemic (a broken image or a
#                        fixture/container start error fails every test in a class) and
#                        the job fails fast instead of re-running a huge set 3 times
#                        (default: 20).

set -uo pipefail

PROJECT="$1"
TEST_FILTER="$2"
shift 2
EXTRA_ARGS=("$@")

MAX_ATTEMPTS="${TEST_MAX_ATTEMPTS:-3}"
MAX_RETRY_TESTS="${TEST_MAX_RETRY_TESTS:-20}"
# This script owns --results-directory (TRX files are read back to find failed tests on retry).
# Callers that also want coverage collected into a known location set TEST_RESULTS_DIR so both the
# TRX files and the coverage results land there — passing a second --results-directory as an extra
# arg would make `dotnet test` fail with "expects a single argument but 2 were provided".
RESULTS_DIR="${TEST_RESULTS_DIR:-$(mktemp -d)}"
# Ledger of tests a retry rescued. Lives in RESULTS_DIR so it rides along with the
# coverage artifact CI already uploads.
FLAKY_LOG="${RESULTS_DIR}/flaky-tests.log"

# Extracts the fully-qualified names of failed tests from a TRX result file.
extract_failed() {
    local trx="$1"
    python3 - "$trx" <<'PY'
import sys, xml.etree.ElementTree as ET
ns = '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
try:
    tree = ET.parse(sys.argv[1])
except Exception:
    sys.exit(0)
names = sorted({
    r.get('testName')
    for r in tree.iter(f'{ns}UnitTestResult')
    if r.get('outcome') == 'Failed' and r.get('testName')
})
if names:
    print('\n'.join(names))
PY
}

# Union of the tests that failed on attempts 1..N, read back from those attempts' TRX files.
failed_up_to() {
    local last="$1" i
    for ((i = 1; i <= last; i++)); do
        extract_failed "${RESULTS_DIR}/attempt-${i}.trx"
    done | sort -u
}

# Records the tests a retry rescued. Without this a flaky test that passes on attempt 2
# leaves no trace at all, so nobody can tell a flaky suite from a healthy one.
record_flaky() {
    local rescued_on="$1"
    shift
    local names=("$@")
    [ "${#names[@]}" -eq 0 ] && return 0

    local timestamp
    timestamp="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

    mkdir -p "$RESULTS_DIR"
    local name
    for name in "${names[@]}"; do
        printf '%s\t%s\tpassed-on-attempt-%s\t%s\n' \
            "$timestamp" "$PROJECT" "$rescued_on" "$name" >> "$FLAKY_LOG"
    done

    local joined
    joined="$(printf '%s, ' "${names[@]}")"
    echo "::warning::${#names[@]} flaky test(s) failed on an earlier attempt and passed on attempt ${rescued_on}: ${joined%, }"
    echo "Recorded ${#names[@]} flaky test(s) in ${FLAKY_LOG}."
}

run() {
    local filter="$1"
    local trx="$2"
    dotnet test "$PROJECT" \
        --no-restore \
        --filter "$filter" \
        --logger "console;verbosity=normal" \
        --logger "trx;LogFileName=$trx" \
        --results-directory "$RESULTS_DIR" \
        --configuration Release \
        "${EXTRA_ARGS[@]}"
}

attempt=1
echo "::group::Test attempt ${attempt}/${MAX_ATTEMPTS} (full filter)"
run "${TEST_FILTER}" "attempt-${attempt}.trx"
status=$?
echo "::endgroup::"

if [ $status -eq 0 ]; then
    echo "All tests passed on attempt ${attempt}."
    exit 0
fi

while [ $attempt -lt "$MAX_ATTEMPTS" ]; do
    failed=()
    while IFS= read -r line; do
        [ -n "$line" ] && failed+=("$line")
    done < <(extract_failed "${RESULTS_DIR}/attempt-${attempt}.trx")

    if [ "${#failed[@]}" -eq 0 ]; then
        # Test run failed but no individual failed test was recorded (e.g. a
        # crash, build, or fixture/container start error). Retrying the whole
        # subset is meaningless and would hide an infrastructure failure — fail.
        echo "Attempt ${attempt} failed but no failed tests were recorded in the TRX. Treating as a hard failure."
        exit 1
    fi

    if [ "${#failed[@]}" -gt "$MAX_RETRY_TESTS" ]; then
        # A large failed set is systemic, not flaky — a broken image or a failing
        # class fixture fails every test in the affected class(es). Re-running it
        # would just multiply the wasted time, so fail fast instead.
        echo "Attempt ${attempt} had ${#failed[@]} failed tests (> ${MAX_RETRY_TESTS}). Treating as a systemic failure; not retrying."
        exit 1
    fi

    attempt=$((attempt + 1))

    echo "::group::Test attempt ${attempt}/${MAX_ATTEMPTS} (re-running ${#failed[@]} failed test(s))"
    printf '  - %s\n' "${failed[@]}"

    filter=""
    for name in "${failed[@]}"; do
        [ -n "$filter" ] && filter+="|"
        filter+="FullyQualifiedName=${name}"
    done

    run "$filter" "attempt-${attempt}.trx"
    status=$?
    echo "::endgroup::"

    if [ $status -eq 0 ]; then
        echo "Previously failed tests passed on attempt ${attempt} — treating as flaky, not a regression."

        rescued=()
        while IFS= read -r line; do
            [ -n "$line" ] && rescued+=("$line")
        done < <(failed_up_to $((attempt - 1)))

        if [ "${#rescued[@]}" -gt 0 ]; then
            record_flaky "$attempt" "${rescued[@]}"
        fi

        exit 0
    fi
done

echo "Tests still failing after ${MAX_ATTEMPTS} attempts."
exit 1
