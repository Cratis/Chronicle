#!/usr/bin/env bash
# PostToolUse hook — deterministic pattern pass over a just-edited file.
#
# Zero model calls, zero token cost until something actually matches. Reads the Claude Code
# hook JSON on stdin, resolves the edited file, and runs every applicable pattern from
# cratis-patterns.json against it. A match appends a one-line reminder to context; it NEVER
# blocks (always exits 0). Each pattern fires at most once per file per session, so it
# cannot flood the conversation.
#
# Escape hatch: CRATIS_HOOKS_SKIP_SCAN=1 disables the pass entirely.
set -euo pipefail

# SCRIPTDIR, not a path relative to the caller: shellcheck resolves a plain relative `source=`
# against the current working directory, and these hooks are linted from wherever CI happens to run.
# shellcheck source=SCRIPTDIR/hook-lib.sh
. "$(dirname "${BASH_SOURCE[0]}")/hook-lib.sh"

[ "${CRATIS_HOOKS_SKIP_SCAN:-0}" = "1" ] && exit 0

input="$(hook_read_stdin)"
[ -n "$input" ] || exit 0

# jq is the repository's established JSON dependency. If it is not
# installed the hook degrades to a silent no-op rather than breaking the session.
hook_have jq || exit 0

root="$(hook_repo_root)"
cwd="$(hook_json "$input" '.cwd')"
[ -n "$cwd" ] || cwd="$root"

file="$(hook_json "$input" '.tool_input.file_path')"
[ -n "$file" ] || file="$(hook_json "$input" '.tool_input.notebook_path')"
[ -n "$file" ] || exit 0

file="$(hook_abspath "$file" "$cwd")"
[ -f "$file" ] || exit 0

# Never scan binaries or very large files — this hook must stay cheap.
LC_ALL=C grep -Iq . "$file" 2>/dev/null || exit 0
size="$(wc -c <"$file" 2>/dev/null || printf '0')"
[ "${size:-0}" -le 2000000 ] || exit 0

rel="$(hook_relpath "$file" "$root")"

# ── Pattern set: shipped defaults + optional local extension / override ───────
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
base_file="${CRATIS_HOOKS_PATTERNS:-$here/cratis-patterns.json}"
local_file="$here/cratis-patterns.local.json"
[ -f "$base_file" ] || exit 0

if [ -f "$local_file" ]; then
    # Local entries with the same id win; new ids are appended.
    merged="$(jq -s '
        {patterns: (
            ((.[0].patterns // []) | map(select(.id as $i | ((.[1].patterns // []) | map(.id) | index($i)) == null)))
            + ((.[1].patterns // []))
        )}' "$base_file" "$local_file" 2>/dev/null || true)"
    [ -n "$merged" ] || merged="$(cat "$base_file")"
else
    merged="$(cat "$base_file")"
fi

# One record per line. Fields separated by US (0x1f); list members inside a field by SOH (0x01).
# `join` is used instead of @tsv because @tsv escapes backslashes, which would corrupt regexes.
records="$(printf '%s' "$merged" | jq -r '
    .patterns // []
    | map(select((.enabled // true) == true))
    | .[]
    | [ .id,
        .pattern,
        .message,
        (.rule // ""),
        ((.paths // ["**/*"]) | join("")),
        ((.exclude_paths // []) | join("")),
        ((.requires_in_file // []) | join("")),
        ((.absent_in_file // []) | join("")),
        (.within_type_attribute // ""),
        ((.join_lines // 1) | tostring) ]
    | join("")
' 2>/dev/null || true)"
[ -n "$records" ] || exit 0

session="$(hook_json "$input" '.session_id')"
state_dir="$(hook_state_dir "${session:-nosession}")" || exit 0
seen_file="$state_dir/pattern-scan.seen"

# Match a file against the newline-separated ERE list on stdin. mode=all|none.
file_has() {
    local mode="$1" f="$2" re
    while IFS= read -r re; do
        [ -n "$re" ] || continue
        if LC_ALL=C grep -Eq -- "$re" "$f" 2>/dev/null; then
            [ "$mode" = "none" ] && return 1
        else
            [ "$mode" = "all" ] && return 1
        fi
    done
    return 0
}

findings=""
while IFS=$'\037' read -r id pattern message rule paths excludes requires absent attr joinlines; do
    [ -n "${id:-}" ] || continue
    [ -n "${pattern:-}" ] || continue

    printf '%s' "${paths:-}" | tr '\001' '\n' | hook_glob_match "$rel" || continue
    if [ -n "${excludes:-}" ]; then
        printf '%s' "$excludes" | tr '\001' '\n' | hook_glob_match "$rel" && continue
    fi
    if [ -n "${requires:-}" ]; then
        printf '%s' "$requires" | tr '\001' '\n' | file_has all "$file" || continue
    fi
    if [ -n "${absent:-}" ]; then
        printf '%s' "$absent" | tr '\001' '\n' | file_has none "$file" || continue
    fi

    # The scan: first matching line number, or nothing. Regexes travel through ENVIRON
    # because `awk -v` performs backslash-escape processing on the value.
    line="$(
        CRATIS_PAT="$pattern" CRATIS_ATTR="${attr:-}" CRATIS_JOIN="${joinlines:-1}" awk '
            function cnt(s, ch,    n, i) { n = 0; for (i = 1; i <= length(s); i++) if (substr(s, i, 1) == ch) n++; return n }
            function trim(s) { sub(/^[ \t]+/, "", s); sub(/[ \t\r]+$/, "", s); return s }
            BEGIN {
                pat  = ENVIRON["CRATIS_PAT"]
                attr = ENVIRON["CRATIS_ATTR"]
                jn   = ENVIRON["CRATIS_JOIN"] + 0
                if (jn < 1) jn = 1
                if (attr != "") attrRe = "\\[" attr "(\\]|\\()"
                mode = 0; depth = 0; pending = ""
            }
            {
                lines[NR] = $0
                if (attr == "") next
                t = trim($0)
                if (mode == 2) {                                  # inside a braced type body
                    scope[NR] = 1
                    depth += cnt($0, "{") - cnt($0, "}")
                    if (depth <= 0) mode = 0
                    next
                }
                if (mode == 1) {                                  # declaration still being read
                    scope[NR] = 1
                    if (index($0, "{") > 0) {
                        depth = cnt($0, "{") - cnt($0, "}")
                        mode = (depth > 0) ? 2 : 0
                    } else if (index($0, ";") > 0) {
                        mode = 0
                    }
                    next
                }
                if (t == "" || t ~ /^\/\// || t ~ /^\*/ || t ~ /^\/\*/) next
                if (t ~ /^\[/) { pending = pending " " t; next }  # accumulate attributes
                if (t ~ /(^|[ \t])(record|class|struct|interface)([ \t]|$)/) {
                    if (attrRe != "" && pending ~ attrRe) {
                        scope[NR] = 1
                        if (index($0, "{") > 0)      { depth = cnt($0, "{") - cnt($0, "}"); mode = (depth > 0) ? 2 : 0 }
                        else if (index($0, ";") > 0) { mode = 0 }
                        else                         { mode = 1 }
                    }
                    pending = ""
                    next
                }
                pending = ""
            }
            END {
                for (i = 1; i <= NR; i++) {
                    if (attr != "" && !(i in scope)) continue
                    tl = trim(lines[i])
                    if (tl ~ /^\/\// || tl ~ /^\*/ || tl ~ /^\/\*/) continue
                    buf = lines[i]
                    for (j = 1; j < jn; j++) if (i + j <= NR) buf = buf " " lines[i + j]
                    if (buf ~ pat) { print i; exit }
                }
            }
        ' "$file" 2>/dev/null || true
    )"
    [ -n "$line" ] || continue

    hook_claim_once "$seen_file" "$id	$rel" || continue

    findings="${findings}${rel}:${line} — [${rule}] ${message}
"
done <<EOF
$records
EOF

[ -n "$findings" ] || exit 0

context="Cratis rule check on the file you just edited (deterministic pattern pass, not a build):
${findings}
These are framework contracts from .ai/rules/general.md. Fix them now rather than at the quality gate."

jq -n --arg ctx "$context" '{
    systemMessage: "cratis-hooks: rule reminder appended to context",
    hookSpecificOutput: { hookEventName: "PostToolUse", additionalContext: $ctx }
}'
exit 0
