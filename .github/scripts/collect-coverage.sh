#!/bin/bash

set -e

# Default values
COVERAGE_REPORT_PATH="${1:-./coverage-reports}"
OUTPUT_PATH="${2:-./Documentation/statistics/coverage-data.js}"
VERSION="${3:-}"
COMMIT_SHA="${4:-}"
COMMIT_MESSAGE="${5:-}"

echo "Collecting coverage data from: $COVERAGE_REPORT_PATH"

# Read existing data or create new structure
if [ -f "$OUTPUT_PATH" ]; then
    # Extract JSON from the file (removing the "window.COVERAGE_DATA = " prefix and trailing semicolon)
    existing_json=$(sed 's/^window\.COVERAGE_DATA = //; s/;$//' "$OUTPUT_PATH")
else
    existing_json='{"lastUpdate":0,"repoUrl":"https://github.com/Cratis/Chronicle","entries":{}}'
fi

# Find the main Summary.json file (reportgenerator creates one at the root)
summary_file="$COVERAGE_REPORT_PATH/Summary.json"

if [ ! -f "$summary_file" ]; then
    echo "Warning: No Summary.json found in $COVERAGE_REPORT_PATH"
    # Still update the timestamp
    timestamp=$(date +%s%3N)
    echo "window.COVERAGE_DATA = $(echo "$existing_json" | jq --argjson ts "$timestamp" '.lastUpdate = $ts');" > "$OUTPUT_PATH"
    echo "Updated timestamp in coverage data (no coverage report found)"
    exit 0
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

# Get current date and week
current_date=$(date +%Y-%m-%d)
year=$(date +%Y)
month=$(date +%m)
day=$(date +%d)
week_of_month=$(( (day + 6) / 7 ))
current_week="${year}-M${month}-W${week_of_month}"

# Extract commit hash (first 7 chars)
commit_short="${COMMIT_SHA:0:7}"

# Get timestamp
timestamp=$(date +%s%3N)

# Process assemblies and update the data structure
result_json=$(echo "$existing_json" | jq --slurpfile summary "$summary_file" \
                  --arg week "$current_week" \
                  --arg date "$current_date" \
                  --arg version "$VERSION" \
                  --arg commit "$commit_short" \
                  --argjson timestamp "$timestamp" '
    # Update timestamp
    .lastUpdate = $timestamp |

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
echo "window.COVERAGE_DATA = $(echo "$result_json" | jq -c .);" > "$OUTPUT_PATH"

# Count projects and show summary
project_count=$(echo "$result_json" | jq '.entries | keys | length')

# Show processed projects
echo "$result_json" | jq -r '.entries | to_entries[] | "  Project: \(.key), Coverage: \(.value[0].lineCoverage)%"'

echo "Coverage data updated successfully"
echo "Output written to: $OUTPUT_PATH"
echo "Total projects tracked: $project_count"
