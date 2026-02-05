#!/bin/bash

# Markdown Verification Script
# This script runs the same markdown linting and link verification that runs in CI

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "=========================================="
echo "Markdown Verification"
echo "=========================================="
echo ""

# Check if running from repository root or Documentation folder
if [ "$(basename "$PWD")" = "Documentation" ]; then
    cd ..
fi

echo "Working directory: $PWD"
echo ""

# Step 1: Markdown Linting
echo "=========================================="
echo "Step 1: Running markdownlint..."
echo "=========================================="
echo ""

if ! command -v npx &> /dev/null; then
    echo "Error: npx is not installed. Please install Node.js and npm."
    exit 1
fi

npx markdownlint-cli2 "Documentation/**/*.md"
LINT_EXIT_CODE=$?

echo ""
if [ $LINT_EXIT_CODE -eq 0 ]; then
    echo "✓ Markdown linting passed!"
else
    echo "✗ Markdown linting failed with exit code $LINT_EXIT_CODE"
fi
echo ""

# Step 2: Link Verification
echo "=========================================="
echo "Step 2: Running link verification..."
echo "=========================================="
echo ""
echo "This may take a few minutes to check all links..."
echo ""

npx linkinator "Documentation/**/*.md" --markdown --recurse --verbosity error --status-code "403:ok"
LINK_EXIT_CODE=$?

echo ""
if [ $LINK_EXIT_CODE -eq 0 ]; then
    echo "✓ Link verification passed!"
else
    echo "✗ Link verification failed with exit code $LINK_EXIT_CODE"
fi
echo ""

# Final summary
echo "=========================================="
echo "Summary"
echo "=========================================="
if [ $LINT_EXIT_CODE -eq 0 ] && [ $LINK_EXIT_CODE -eq 0 ]; then
    echo "✓ All checks passed!"
    exit 0
else
    echo "✗ Some checks failed:"
    [ $LINT_EXIT_CODE -ne 0 ] && echo "  - Markdown linting"
    [ $LINK_EXIT_CODE -ne 0 ] && echo "  - Link verification"
    exit 1
fi
