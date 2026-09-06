#!/usr/bin/env bash
# Shared helpers for the Cratis enforcement hooks.
#
# Sourced by cratis-guard-writes.sh, cratis-pattern-scan.sh and cratis-quality-gate.sh.
# Portable: bash 3.2 (macOS system bash) and up, BSD + GNU userland. No GNU-only flags,
# no `mapfile`/`readarray`, no associative arrays, no `eval`.
set -euo pipefail

# ── Environment ───────────────────────────────────────────────────────────────

# Root of the repository the hook is running for.
hook_repo_root() {
    local d="${CLAUDE_PROJECT_DIR:-}"
    if [ -n "$d" ] && [ -d "$d" ]; then
        (cd "$d" && pwd)
        return 0
    fi
    (cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)
}

# True when the named command is on PATH.
hook_have() {
    command -v "$1" >/dev/null 2>&1
}

# Read all of stdin. Never fails on an empty pipe.
hook_read_stdin() {
    cat 2>/dev/null || true
}

# Extract a single scalar from a JSON document on $1 using the jq path in $2.
# Prints the empty string when jq is absent, the JSON is malformed, or the path is null.
hook_json() {
    local json="$1" path="$2"
    hook_have jq || return 0
    printf '%s' "$json" | jq -r "$path // empty" 2>/dev/null || true
}

# ── Paths ─────────────────────────────────────────────────────────────────────

# Absolutize $1 against $2 (a directory), leaving already-absolute paths untouched.
hook_abspath() {
    local p="$1" base="${2:-$PWD}"
    case "$p" in
        /*) printf '%s\n' "$p" ;;
        *) printf '%s\n' "${base%/}/$p" ;;
    esac
}

# Make $1 relative to the root $2 when it lives underneath it.
hook_relpath() {
    local p="$1" r="${2%/}"
    case "$p" in
        "$r"/*) printf '%s\n' "${p#"$r"/}" ;;
        *) printf '%s\n' "$p" ;;
    esac
}

# Does path $1 match any of the newline-separated globs on stdin?
#
# Glob dialect (gitignore/minimatch-like, matched against a repo-relative path):
#   **/   any number of leading directories (including none)
#   **    any characters, directory separators included
#   *     any characters except /
#   ?     one character except /
hook_glob_match() {
    CRATIS_GLOB_PATH="$1" awk '
        function g2re(g,    out, i, n, c, c2, c3) {
            out = "^"; n = length(g)
            for (i = 1; i <= n; i++) {
                c = substr(g, i, 1)
                if (c == "*") {
                    c2 = substr(g, i + 1, 1); c3 = substr(g, i + 2, 1)
                    if (c2 == "*" && c3 == "/") { out = out "(.*/)?"; i += 2 }
                    else if (c2 == "*")         { out = out ".*";     i += 1 }
                    else                        { out = out "[^/]*" }
                } else if (c == "?") {
                    out = out "[^/]"
                } else if (index(".[]()+^$\\{}|", c) > 0) {
                    out = out "\\" c
                } else {
                    out = out c
                }
            }
            return out "$"
        }
        BEGIN { p = ENVIRON["CRATIS_GLOB_PATH"]; found = 0 }
        { g = $0; sub(/^[ \t]+/, "", g); sub(/[ \t\r]+$/, "", g) }
        g == "" { next }
        { if (p ~ g2re(g)) { found = 1; exit } }
        END { exit(found ? 0 : 1) }
    '
}

# ── Session state ─────────────────────────────────────────────────────────────

# Per-session scratch directory used to de-duplicate reminders. Session ids are
# sanitized so a hostile value cannot escape the scratch root.
hook_state_dir() {
    local session="${1:-nosession}" base
    session="$(printf '%s' "$session" | tr -c 'A-Za-z0-9._-' '_')"
    [ -n "$session" ] || session="nosession"
    base="${TMPDIR:-/tmp}"
    base="${base%/}/cratis-hooks/$session"
    mkdir -p "$base" 2>/dev/null || return 1
    printf '%s\n' "$base"
}

# Record key $2 in the state file $1. Returns 0 the first time, 1 afterwards.
hook_claim_once() {
    local file="$1" key="$2"
    if [ -f "$file" ] && LC_ALL=C grep -Fqx -- "$key" "$file" 2>/dev/null; then
        return 1
    fi
    printf '%s\n' "$key" >>"$file" 2>/dev/null || true
    return 0
}
