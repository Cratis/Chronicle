#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Script to compare two gRPC schemas and detect breaking changes
# Usage: ./compare-grpc-schemas.sh <baseline-schema> <current-schema>

set -e

BASELINE_SCHEMA="$1"
CURRENT_SCHEMA="$2"

if [ ! -f "$BASELINE_SCHEMA" ]; then
    echo "Error: Baseline schema file not found: $BASELINE_SCHEMA"
    exit 1
fi

if [ ! -f "$CURRENT_SCHEMA" ]; then
    echo "Error: Current schema file not found: $CURRENT_SCHEMA"
    exit 1
fi

echo "Comparing gRPC schemas..."
echo "Baseline: $BASELINE_SCHEMA"
echo "Current: $CURRENT_SCHEMA"
echo ""

# Parse schemas and compare them
parse_schema() {
    local schema_file="$1"
    local service=""
    
    while IFS= read -r line; do
        # Detect service definition
        if echo "$line" | grep -q "^[[:space:]]*service[[:space:]]"; then
            service=$(echo "$line" | sed -n 's/^[[:space:]]*service[[:space:]]\+\([A-Za-z0-9_]\+\).*/\1/p')
            if [ -n "$service" ]; then
                echo "service:$service"
            fi
        # Detect RPC method
        elif echo "$line" | grep -q "^[[:space:]]*rpc[[:space:]]"; then
            method=$(echo "$line" | sed -n 's/^[[:space:]]*rpc[[:space:]]\+\([A-Za-z0-9_]\+\).*/\1/p')
            request=$(echo "$line" | sed -n 's/.*([[:space:]]*\([^)]\+\)[[:space:]]*).*/\1/p' | head -1)
            response=$(echo "$line" | sed -n 's/.*returns[[:space:]]*([[:space:]]*\([^)]\+\)[[:space:]]*).*/\1/p')
            if [ -n "$method" ]; then
                echo "method:$service.$method:$request:$response"
            fi
        fi
    done < "$schema_file"
}

# Parse both schemas
BASELINE_ITEMS=$(parse_schema "$BASELINE_SCHEMA")
CURRENT_ITEMS=$(parse_schema "$CURRENT_SCHEMA")

# Check for breaking changes
BREAKING_CHANGES=()

# Check for removed services
while IFS= read -r line; do
    if echo "$line" | grep -q "^service:"; then
        service=$(echo "$line" | cut -d: -f2)
        if ! echo "$CURRENT_ITEMS" | grep -q "^service:$service\$"; then
            BREAKING_CHANGES+=("Service '$service' was removed")
        fi
    fi
done <<< "$BASELINE_ITEMS"

# Check for removed or modified methods
while IFS= read -r line; do
    if echo "$line" | grep -q "^method:"; then
        method=$(echo "$line" | cut -d: -f2)
        request=$(echo "$line" | cut -d: -f3)
        response=$(echo "$line" | cut -d: -f4)
        
        # Check if method exists in current schema
        if ! echo "$CURRENT_ITEMS" | grep -q "^method:$method:"; then
            BREAKING_CHANGES+=("Method '$method' was removed")
        else
            # Check if signature changed
            if ! echo "$CURRENT_ITEMS" | grep -q "^$line\$"; then
                BREAKING_CHANGES+=("Method '$method' signature changed from ($request) returns ($response)")
            fi
        fi
    fi
done <<< "$BASELINE_ITEMS"

# Output results
if [ ${#BREAKING_CHANGES[@]} -eq 0 ]; then
    echo "✅ No breaking changes detected"
    exit 0
else
    echo "⚠️  Breaking changes detected:"
    echo ""
    for change in "${BREAKING_CHANGES[@]}"; do
        echo "  - $change"
    done
    exit 1
fi
