#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Script to update PR description with gRPC breaking change warning
# Usage: ./update-pr-description.sh <pr-number> <breaking-changes>

set -e

PR_NUMBER="$1"
BREAKING_CHANGES="$2"

if [ -z "$PR_NUMBER" ]; then
    echo "Error: PR number not provided"
    exit 1
fi

if [ -z "$BREAKING_CHANGES" ]; then
    echo "No breaking changes to report"
    exit 0
fi

# Marker to identify our warning
WARNING_START="<!-- grpc-breaking-changes-warning-start -->"
WARNING_END="<!-- grpc-breaking-changes-warning-end -->"

# Get current PR description
CURRENT_DESCRIPTION=$(gh pr view "$PR_NUMBER" --json body --jq '.body')

# Check if warning already exists
if echo "$CURRENT_DESCRIPTION" | grep -q "$WARNING_START"; then
    echo "Warning already exists in PR description"
    exit 0
fi

# Format breaking changes
IFS=';' read -ra CHANGES <<< "$BREAKING_CHANGES"
FORMATTED_CHANGES=""
for change in "${CHANGES[@]}"; do
    FORMATTED_CHANGES="$FORMATTED_CHANGES- $change\n"
done

# Create warning message
WARNING="$WARNING_START
## ⚠️ gRPC API Breaking Changes Detected

This PR introduces **breaking changes** to the gRPC API surface that will cause wire incompatibility with existing clients.

### Breaking Changes:
$FORMATTED_CHANGES
Please ensure:
1. This is intentional and documented in the release notes
2. Version number is bumped appropriately (major version for breaking changes)
3. Migration guide is provided if necessary
$WARNING_END

"

# Prepend warning to current description
NEW_DESCRIPTION="$WARNING
$CURRENT_DESCRIPTION"

# Update PR description
echo "Updating PR description with breaking change warning..."
echo "$NEW_DESCRIPTION" | gh pr edit "$PR_NUMBER" --body-file -

echo "PR description updated successfully"
