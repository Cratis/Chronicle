#!/usr/bin/env bash
# cratis-ai-managed: hooks/scripts/cratis-guard-pr-body.sh
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
# PreToolUse (Bash): release-note bodies are checked before gh pr create/edit.
# Node reads the hook JSON and tokenizes literal shell arguments without executing them.
set -euo pipefail
# Read the inherited descriptor directly: Node uses a socket for stdin on Linux,
# and reopening it via /dev/stdin fails with ENXIO. EOF without a newline is normal.
payload=''
IFS= read -r -d '' payload || true
# This dependency-free prefilter runs even on Node-free machines. Normalize JSON-escaped
# line continuations before matching; Node still determines executable command identity.
command_text=${payload//\\n/ }
command_text=${command_text//\\t/ }
command_text=${command_text//\\r/ }
command_text=${command_text//\\/}
if [[ ! "$command_text" =~ gh[[:space:]]+pr[[:space:]]+(create|edit) ]]; then
    exit 0
fi
if ! command -v node >/dev/null 2>&1; then
    # No warning for unrelated repositories or installations without release workflows.
    origin=$(git remote get-url origin 2>/dev/null || true)
    if [[ ! "$origin" =~ ^(https?://github.com/|(ssh://)?git@github.com[:/])Cratis/ ]]; then
        exit 0
    fi
    root=$(git rev-parse --show-toplevel 2>/dev/null || true)
    if ! grep -Eq "^[[:space:]]*uses:[[:space:]]*['\"]?Cratis/Workflows/\\.github/workflows/verify-(release-notes|semver-label|release-intent)\\.yml@" "$root"/.github/workflows/*.yml "$root"/.github/workflows/*.yaml 2>/dev/null; then
        exit 0
    fi
    # JSON makes fail-open warnings visible to Claude; stderr exposes the same warning to Pi.
    printf '%s\n' '{"systemMessage":"Warning: unchecked pull-request body: node is unavailable.","hookSpecificOutput":{"hookEventName":"PreToolUse","additionalContext":"Warning: unchecked pull-request body: node is unavailable."}}'
    printf 'Warning: unchecked pull-request body: node is unavailable.\n' >&2
    exit 0
fi
status=0
node "$(dirname "${BASH_SOURCE[0]}")/cratis-check-pr.mjs" --hook <<< "$payload" || status=$?
# Unexpected runtime failures are not an allow verdict. Only the checker handles the
# documented offline/no-cache pass-through; its hook mode translates violations to 2.
if [ "$status" -ne 0 ]; then
    exit 2
fi
