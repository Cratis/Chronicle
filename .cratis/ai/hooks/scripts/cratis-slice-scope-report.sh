#!/usr/bin/env bash
# cratis-ai-managed: hooks/scripts/cratis-slice-scope-report.sh
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
# Advisory only: read repo-relative changed paths on stdin, report conventional application slices.
# Usage: bash cratis-slice-scope-report.sh <repository-root>
set -euo pipefail

root="$1"
changed="$(cat)"
[ -n "$changed" ] || exit 0

# Require a same-name, model-bound C# anchor; a four-segment directory alone proves no slice.
# Unrecognized layouts stay silent rather than guessing a feature root or a framework profile.
candidates="$(printf '%s\n' "$changed" | awk -F/ '
    $1 == "Source" && NF >= 5 && $2 != "" && $3 != "" && $4 != "" {
        path = $1 "/" $2 "/" $3 "/" $4
        if (!seen[path]++) print path
    }
')"
[ -n "$candidates" ] || exit 0
paths="$(git -C "$root" ls-files && git -C "$root" ls-files --others --exclude-standard)"
while IFS= read -r slice; do
    anchor="$slice/${slice##*/}.cs"
    [ -f "$root/$anchor" ] || continue
    LC_ALL=C grep -Eq '^[[:space:]]*\[(Command|ReadModel)(Attribute)?([[:space:]]*\(|[[:space:]]*\])' "$root/$anchor" || continue
    printf 'cratis-quality-gate: ADVISORY slice scope %s\n' "$slice" >&2
    while IFS= read -r path; do
        case "$path" in
            "$slice"/*) ;;
            *) printf '  seam or out of scope: %s\n' "$path" >&2 ;;
        esac
    done <<EOF
$changed
EOF
    if printf '%s\n' "$changed" | LC_ALL=C grep -Fxq "$anchor"; then
        spec_paths="$(printf '%s\n' "$paths" | CRATIS_SLICE="$slice/" awk '
            index($0, ENVIRON["CRATIS_SLICE"]) == 1 && /\/when_[^\/]+(\/|\.cs$)/ { print }
        ')"
        has_specs=0
        while IFS= read -r path; do
            if [ -n "$path" ] && [ -f "$root/$path" ]; then
                has_specs=1
            fi
        done <<EOF
$spec_paths
EOF
        if [ "$has_specs" -eq 0 ]; then
            printf '  specification folder not found for changed %s (expected when_* under this slice)\n' "$anchor" >&2
        fi
    fi
done <<EOF
$candidates
EOF
exit 0
