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

# Pinned VSTest 18.10.1 (87dfd4b, DotnetTestHostManager.cs:307,635) uses
# the test assembly directory as cwd. This Client leg has default net10.0 Release
# output; Orleans FileLogger appends relative to that directory, not results/cwd.
subscription_diagnostic_logs() {
    printf '%s\n' "$1"/[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9][0-9][0-9].log
}

collect_subscription_diagnostics() {
    local root="$1" invocation="$2" output="$3"
    local LC_ALL=C
    export LC_ALL
    local file size identity offset delta index markers total_bytes=0 output_bytes=0 owned_files=0
    local files=()
    while IFS= read -r file; do files+=("$file"); done < <(subscription_diagnostic_logs "$root")
    if (( ${#files[@]} > 32 )); then
        echo "#5354 diagnostic evidence incomplete: file limit exceeded."
        return 2
    fi
    if ! printf '#5354 invocation %s: partial marker-only evidence; missing markers are incomplete, not proof of success.\n' "$invocation" > "$output"; then return 2; fi
    for file in "${diagnostic_files[@]}"; do
        if [[ ! -f "$file" ]]; then
            echo "#5354 diagnostic evidence incomplete: a snapshotted log disappeared."
            return 2
        fi
    done
    for file in "${files[@]}"; do
        [[ -f "$file" ]] || continue
        [[ ! -L "$file" ]] || return 2
        if ! identity=$(stat -c '%d:%i:%s' "$file"); then return 2; fi
        size="${identity##*:}"
        identity="${identity%:*}"
        offset=0
        for ((index=0; index<${#diagnostic_files[@]}; index++)); do
            if [[ "${diagnostic_files[$index]}" == "$file" ]]; then
                if [[ "${diagnostic_identities[$index]}" != "$identity" ]]; then
                    echo "#5354 diagnostic evidence incomplete: a snapshotted log rotated."
                    return 2
                fi
                offset="${diagnostic_sizes[$index]}"
                break
            fi
        done
        delta=$((size - offset))
        (( delta == 0 )) && continue
        total_bytes=$((total_bytes + delta))
        if (( delta < 0 || delta > 4194304 || total_bytes > 16777216 )); then
            echo "#5354 diagnostic evidence incomplete: truncated log or byte limit exceeded."
            return 2
        fi
        # GNU dd on the actual ubuntu-latest leg bounds reads even if a writer
        # appends after the size observation. Raw bytes never reach output/results.
        if ! markers=$(dd if="$file" bs=65536 iflag=skip_bytes,count_bytes skip="$offset" count="$delta" status=none | awk -F '\t' -v invocation="$invocation" '
            function fixture() { return $4 ~ /^Cratis[.]Chronicle[.]XUnit[.]Integration[.]ChronicleOrleansFixture([<`]|\])/ }
            function reset() { return fixture() && $5 ~ /^Fixture kernel reset [0-9a-fA-F-]+ for invocation / }
            reset() {
                if (index($5, "for invocation \047" invocation "\047 ") == 0) mixed = 1
                else owned = 1
            }
            {
                message = $5
                allowed = reset() && message ~ / (starting before storage wipes|completed both storage wipes)/
                if ($4 == "Cratis.Chronicle.Observation.EventStoreSubscriptions.EventStoreSubscriptionsManager]") {
                    allowed = message ~ /^Subscription manager (activated for target |memory read for target )/ ||
                        message ~ /^Subscription definition (write starting for target |WriteStateAsync completed for target )/
                }
                if ($4 == "Cratis.Chronicle.Services.Observation.EventStoreSubscriptions.EventStoreSubscriptions]") {
                    allowed = message ~ /^Subscription registration [0-9a-fA-F-]+ for target / ||
                        message ~ /^Existing persisted subscription query completed for target /
                }
                if (allowed) {
                    if (++count > 256) overflow = 1
                    else lines[count] = $1 "\t" message
                }
            }
            END {
                if (!owned || mixed || overflow) exit 2
                for (i = 1; i <= count; i++) print lines[i]
            }
        '); then
            echo "#5354 diagnostic evidence incomplete: no exclusive invocation reset marker, mixed ownership, read failure or marker limit."
            return 2
        fi
        output_bytes=$((output_bytes + ${#markers} + 1))
        # Reserve space for the bounded invocation header below the 256 KiB cap.
        if (( output_bytes > 260000 )); then
            echo "#5354 diagnostic evidence incomplete: output limit exceeded."
            return 2
        fi
        if ! printf '%s\n' "$markers" >> "$output"; then return 2; fi
        owned_files=$((owned_files + 1))
    done
    if (( owned_files == 0 )); then
        echo "#5354 diagnostic evidence incomplete: no current-invocation marker files."
        return 2
    fi
    echo "#5354 invocation $invocation: marker-only evidence from $owned_files files; not proof of reset success or cross-process correlation."
    cat "$output"
}

run() {
    local filter="$1"
    local trx="$2"
    local invocation="$$-$RANDOM-$RANDOM-$RANDOM" diagnostic_root="" file size test_status
    local diagnostic_files=() diagnostic_sizes=() diagnostic_identities=()
    # This exact leg is invoked by integration.yml:179. No checkout/home scan.
    if [[ "$PROJECT" == "Integration/Client/Client.csproj" ]]; then
        diagnostic_root="$(dirname "$PROJECT")/bin/Release/net10.0"
        local candidates=()
        while IFS= read -r file; do candidates+=("$file"); done < <(subscription_diagnostic_logs "$diagnostic_root")
        if [[ -L Integration || -L Integration/Client || -L Integration/Client/bin || -L Integration/Client/bin/Release || -L "$diagnostic_root" ]]; then
            echo "#5354 diagnostic evidence incomplete: refusing a symlinked output path."
            diagnostic_root=""
        elif (( ${#candidates[@]} > 32 )); then
            echo "#5354 diagnostic evidence incomplete: snapshot file limit exceeded."
            diagnostic_root=""
        else
            for file in "${candidates[@]}"; do
                [[ -f "$file" ]] || continue
                if [[ -L "$file" ]] || ! size=$(stat -c '%d:%i:%s' "$file"); then
                    echo "#5354 diagnostic evidence incomplete: cannot snapshot log."
                    diagnostic_root=""
                    break
                fi
                diagnostic_files+=("$file")
                diagnostic_sizes+=("${size##*:}")
                diagnostic_identities+=("${size%:*}")
            done
        fi
    fi
    CHRONICLE_SUBSCRIPTION_DIAGNOSTICS_INVOCATION="$invocation" dotnet test "$PROJECT" \
        --no-restore \
        --filter "$filter" \
        --logger "console;verbosity=normal" \
        --logger "trx;LogFileName=$trx" \
        --results-directory "$RESULTS_DIR" \
        --configuration Release \
        "${EXTRA_ARGS[@]}"
    test_status=$?
    if [[ -n "$diagnostic_root" ]]; then
        if ! collect_subscription_diagnostics "$diagnostic_root" "$invocation" "$RESULTS_DIR/${trx%.trx}-subscription-diagnostics.log"; then
            echo "#5354 diagnostic evidence incomplete; original test exit status remains $test_status."
        fi
    fi
    return "$test_status"
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
