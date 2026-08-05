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

# Markdown Linting
echo "=========================================="
echo "Running markdownlint..."
echo "=========================================="
echo ""

if ! command -v npx &> /dev/null; then
    echo "Error: npx is not installed. Please install Node.js and npm."
    exit 1
fi

LINT_EXIT_CODE=0
npx markdownlint-cli2 "Documentation/**/*.md" || LINT_EXIT_CODE=$?

echo ""
if [ $LINT_EXIT_CODE -eq 0 ]; then
    echo "✓ Markdown linting passed!"
else
    echo "✗ Markdown linting failed with exit code $LINT_EXIT_CODE"
fi
echo ""

# Link verification lives in the Documentation repo, not here. These pages are
# published through the aggregated docs site, where a link may be a site-level
# route (/chronicle/...), or an extension-less path to a .mdx page. Only the site
# build knows that routing; a crawler pointed at this folder cannot resolve any
# of it and reports every such link as broken. Run `npm run check` in the
# Documentation repo, which must end with 0 errors and 0 broken links.

echo "=========================================="
echo "Summary"
echo "=========================================="
if [ $LINT_EXIT_CODE -eq 0 ]; then
    echo "✓ All checks passed!"
    echo ""
    echo "Links are verified by 'npm run check' in the Documentation repo."
    exit 0
else
    echo "✗ Markdown linting failed."
    exit 1
fi
