#!/bin/bash

set -e
set -o pipefail

# Default values
COVERAGE_REPORT_PATH="${1:-./coverage-reports}"
OUTPUT_PATH="${2:-./Documentation/statistics/coverage-data.js}"
VERSION="${3:-}"
COMMIT_SHA="${4:-}"
# Retain the optional fifth argument for existing callers.
# shellcheck disable=SC2034
COMMIT_MESSAGE="${5:-}"

echo "Collecting coverage data from: $COVERAGE_REPORT_PATH"

# Environment (both optional):
#   COVERAGE_NOW_MS          Override the current time in milliseconds (used by the specs).
#   COVERAGE_MEASURED_AT_MS  Time the coverage was actually measured, in milliseconds. Wins over the
#                            report's own generation time.
# A run without a usable coverage report never advances the measurement date. It only records
# an explicit "missing" or "incomplete" attempt in latestAttempt.

if [ -n "${COVERAGE_NOW_MS:-}" ]; then
    now_ms="$COVERAGE_NOW_MS"
elif command -v python3 >/dev/null 2>&1; then
    now_ms=$(python3 -c "import time; print(int(time.time() * 1000))")
else
    # Fallback: use seconds if Python is not available
    now_ms=$(($(date +%s) * 1000))
fi

# Reject invalid clocks before any output can be overwritten. Never evaluate environment values as JSON.
validate_timestamp() {
    [[ "$1" =~ ^[0-9]+$ ]] || return 1
    jq -en --arg value "$1" '$value | tonumber | select(. >= 0 and . <= 8640000000000000 and . == floor)' >/dev/null
}
validate_timestamp "$now_ms"
if [ -n "${COVERAGE_MEASURED_AT_MS:-}" ]; then
    validate_timestamp "$COVERAGE_MEASURED_AT_MS"
fi

# Extract commit hash (first 7 chars)
commit_short="${COMMIT_SHA:0:7}"

# Read existing data or create new structure
if [ -f "$OUTPUT_PATH" ]; then
    # Extract JSON from the file (removing the "window.COVERAGE_DATA = " prefix and trailing semicolon)
    existing_json=$(sed 's/^window\.COVERAGE_DATA = //; s/;$//' "$OUTPUT_PATH")
else
    existing_json='{"lastUpdate":0,"repoUrl":"https://github.com/Cratis/Chronicle","entries":{}}'
fi

# Validate stored data before either update path; an empty or multi-value file is not a data object.
existing_json=$(printf '%s\n' "$existing_json" | jq -es '
    if length == 1 and (.[0] | type) == "object" then .[0]
    else error("expected one coverage data object") end
')

# Complete JSON production and validation before opening the destination file.
write_data() {
    local formatted_json
    formatted_json=$(printf '%s\n' "$1" | jq -e 'if type == "object" then . else error("expected coverage data object") end')
    printf 'window.COVERAGE_DATA = %s;\n' "$formatted_json" > "$OUTPUT_PATH"
}

# Record an attempt that did not produce a measurement. Leaves lastUpdate, measurement and entries untouched.
record_attempt() {
    local state="$1"
    local reason="$2"
    local attempt_json
    attempt_json=$(printf '%s\n' "$existing_json" | jq --arg state "$state" --arg reason "$reason" \
        --arg version "$VERSION" --arg commit "$commit_short" --argjson now "$now_ms" '
        .schemaVersion = 2 |
        .latestAttempt = {state: $state, attemptedAt: $now, version: $version, commit: $commit, reason: $reason}
    ')
    write_data "$attempt_json"
    # Both arguments are static internal strings, never report paths or other uncontrolled data.
    echo "::warning::Coverage $state: $reason; measurement date unchanged"
    exit 0
}

# Find the main Summary.json file (reportgenerator creates one at the root)
summary_file="$COVERAGE_REPORT_PATH/Summary.json"

if [ ! -f "$summary_file" ]; then
    record_attempt missing "no Summary.json found"
fi

echo "Processing coverage report: $summary_file"

# Validate the JSON file first
if ! jq empty "$summary_file" 2>/dev/null; then
    echo "Error: Invalid JSON in $summary_file"
    echo "Content of file:"
    cat "$summary_file"
    echo ""
    echo "Attempting to fix common issues..."

    # Try to fix common JSON issues (NaN, Infinity, etc.)
    # Replace NaN and Infinity with null or 0
    tmp_file=$(mktemp)
    sed 's/: NaN/: 0/g; s/: Infinity/: 100/g; s/: -Infinity/: 0/g' "$summary_file" > "$tmp_file"

    if jq empty "$tmp_file" 2>/dev/null; then
        echo "Successfully fixed JSON issues"
        mv "$tmp_file" "$summary_file"
    else
        echo "Could not fix JSON issues. File content:"
        cat "$tmp_file"
        rm -f "$tmp_file"
        exit 1
    fi
fi

# Production assemblies after filtering out specs and test infrastructure
assembly_filter='(.coverage.assemblies // []) | map(select(
    (.name | test("\\.Specs$") | not) and
    (.name | test("^xunit") | not) and
    (.name | test("^NSubstitute") | not) and
    (.name | test("^testhost") | not) and
    (.name | test("^coverlet") | not)
)) | length'
production_count=$(jq "$assembly_filter" "$summary_file")
if [ "$production_count" -eq 0 ]; then
    record_attempt incomplete "no production assemblies in Summary.json"
fi

# Determine when the coverage was measured: explicit override, then a validated report date.
generated_on=$(jq -r '.summary.generatedon // empty' "$summary_file")
measured_ms=""
if [[ "${COVERAGE_MEASURED_AT_MS:-}" =~ ^[0-9]+$ ]]; then
    measured_ms="$COVERAGE_MEASURED_AT_MS"
    echo "Measurement time from COVERAGE_MEASURED_AT_MS"
elif [[ "$generated_on" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2} ]]; then
    # Only UTC (no offset, Z or +00:00) is trusted. Reject calendar dates jq normalizes.
    if [[ "$generated_on" =~ ^[0-9T:-]{19}(\.[0-9]+)?(Z|\+00:00)?$ ]]; then
        measured_ms=$(jq -rn --arg v "${generated_on:0:19}Z" '
            try ($v | fromdateiso8601 | select((strftime("%Y-%m-%dT%H:%M:%SZ")) == $v) | . * 1000)
            catch empty
        ')
    fi
fi
if [ -z "$measured_ms" ]; then
    record_attempt incomplete "no trusted measurement timestamp"
else
    echo "Measurement time: $measured_ms"
fi

# Derive date and week from the measurement time in UTC
current_date=$(jq -rn --argjson ms "$measured_ms" '($ms / 1000 | floor | gmtime | strftime("%Y-%m-%d"))')
year=${current_date:0:4}
month=${current_date:5:2}
day=${current_date:8:2}
week_of_month=$(( (10#$day + 6) / 7 ))
current_week="${year}-M${month}-W${week_of_month}"

# Process assemblies and update the data structure
result_json=$(echo "$existing_json" | jq --slurpfile summary "$summary_file" \
                  --arg week "$current_week" \
                  --arg date "$current_date" \
                  --arg version "$VERSION" \
                  --arg commit "$commit_short" \
                  --argjson measured "$measured_ms" \
                  --argjson now "$now_ms" '
    # Record the measurement and the attempt
    .schemaVersion = 2 |
    .lastUpdate = $measured |
    .measurement = {measuredAt: $measured, commit: $commit, version: $version} |
    .latestAttempt = {state: "measured", attemptedAt: $now, version: $version, commit: $commit} |

    # Process each assembly
    ($summary[0].coverage.assemblies // [] | map(
        select(
            (.name | test("\\.Specs$") | not) and
            (.name | test("^xunit") | not) and
            (.name | test("^NSubstitute") | not) and
            (.name | test("^testhost") | not) and
            (.name | test("^coverlet") | not)
        ) | {
            name: .name,
            # Handle coverage value - ensure it is a valid number and convert to percentage
            coverage: (
                (.coverage // 0) as $cov |
                if ($cov | type) != "number" then
                    0
                elif $cov < 1 then
                    (($cov * 100 * 100) | floor / 100)
                else
                    (($cov * 100) | floor / 100)
                end
            )
        }
    )) as $assemblies |

    # Update entries for each assembly
    reduce $assemblies[] as $assembly (
        .;
        # Initialize project entry if needed
        if .entries[$assembly.name] == null then
            .entries[$assembly.name] = []
        else
            .
        end |

        # Check if entry exists for this week
        ((.entries[$assembly.name] // []) | map(select(.week == $week)) | length) as $exists |

        if $exists == 0 then
            # Add new entry
            .entries[$assembly.name] += [{
                date: $date,
                week: $week,
                lineCoverage: $assembly.coverage,
                version: $version,
                commit: $commit
            }]
        else
            # Update existing entry
            .entries[$assembly.name] = [.entries[$assembly.name][] |
                if .week == $week then
                    .lineCoverage = $assembly.coverage |
                    .version = $version |
                    .commit = $commit
                else
                    .
                end
            ]
        end
    ) |

    # Keep only last 52 weeks of data per project
    .entries = (.entries | with_entries(
        .value |= (sort_by(.date) | reverse | .[0:52])
    ))
')

# Write to file
write_data "$result_json"

# Count projects and show summary
project_count=$(echo "$result_json" | jq '.entries | keys | length')

# Show processed projects
echo "$result_json" | jq -r '.entries | to_entries[] | "  Project: \(.key), Coverage: \(.value[0].lineCoverage)%"'

echo "Coverage data updated successfully"
echo "Output written to: $OUTPUT_PATH"
echo "Total projects tracked: $project_count"
