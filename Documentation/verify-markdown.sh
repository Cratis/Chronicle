#!/bin/bash

# Markdown Verification Script
# This script runs the same Markdown linting and authoring validation that runs in CI.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

printf '%s\n' \
    "==========================================" \
    "Markdown Verification" \
    "==========================================" \
    ""

# Both checks address paths from the repository root, whether this script is run
# from there or from the Documentation folder.
cd "$ROOT_DIR"

echo "Working directory: $PWD"
echo ""

printf '%s\n' \
    "==========================================" \
    "Step 1: Running markdownlint..." \
    "==========================================" \
    ""

if ! command -v npx &> /dev/null; then
    echo "Error: npx is not installed. Please install Node.js and npm."
    exit 1
fi

if npx --yes markdownlint-cli2 "Documentation/**/*.{md,mdx}"; then
    LINT_EXIT_CODE=0
else
    LINT_EXIT_CODE=$?
fi

printf '\n'
if [ "$LINT_EXIT_CODE" -eq 0 ]; then
    echo "✓ Markdown linting passed!"
else
    echo "✗ Markdown linting failed with exit code $LINT_EXIT_CODE"
fi
printf '\n'

printf '%s\n' \
    "==========================================" \
    "Step 2: Validating Starlight authoring..." \
    "==========================================" \
    ""

if node "$SCRIPT_DIR/verify-authoring.mjs"; then
    AUTHORING_EXIT_CODE=0
else
    AUTHORING_EXIT_CODE=$?
fi
printf '\n'

printf '%s\n' \
    "==========================================" \
    "Summary" \
    "=========================================="

if [ "$LINT_EXIT_CODE" -eq 0 ] && [ "$AUTHORING_EXIT_CODE" -eq 0 ]; then
    echo "✓ All checks passed!"
    echo ""
    echo "Published-site links are verified by 'npm run check' in the Documentation repo."
    exit 0
fi

echo "✗ Some checks failed:"
[ "$LINT_EXIT_CODE" -ne 0 ] && echo "  - Markdown linting"
[ "$AUTHORING_EXIT_CODE" -ne 0 ] && echo "  - Starlight authoring validation"
exit 1
