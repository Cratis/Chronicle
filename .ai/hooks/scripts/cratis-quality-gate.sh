#!/usr/bin/env bash
# Stop hook — the real quality gate.
#
# Looks at what actually changed in the working tree, runs only the gates that change touches,
# and exits 2 (blocking turn-end, stderr fed back to the model) when one fails. It never edits
# code: it only builds, tests and lints. If nothing relevant changed it exits immediately and
# silently, so a documentation- or corpus-only turn costs nothing.
#
# Gate commands are data (quality-gates.json), not code — see that file for the schema.
#
# Environment:
#   CRATIS_HOOKS_SKIP_GATE=1     skip the gate entirely
#   CRATIS_HOOKS_GATE_DRYRUN=1   print the dispatch plan (which gates would run, and why) and exit 0
#   CRATIS_HOOKS_GATES=<path>    use a different gate configuration file
set -euo pipefail

# SCRIPTDIR, not a path relative to the caller: shellcheck resolves a plain relative `source=`
# against the current working directory, and these hooks are linted from wherever CI happens to run.
# shellcheck source=SCRIPTDIR/hook-lib.sh
. "$(dirname "${BASH_SOURCE[0]}")/hook-lib.sh"

[ "${CRATIS_HOOKS_SKIP_GATE:-0}" = "1" ] && exit 0

input="$(hook_read_stdin)"
hook_have jq || exit 0

# Never re-enter: Claude Code sets stop_hook_active when the previous Stop hook already
# blocked and the model is continuing. Blocking again would loop forever.
if [ -n "$input" ] && [ "$(hook_json "$input" '.stop_hook_active')" = "true" ]; then
    exit 0
fi

root="$(hook_repo_root)"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
config="${CRATIS_HOOKS_GATES:-$here/quality-gates.json}"
dryrun="${CRATIS_HOOKS_GATE_DRYRUN:-0}"

[ -f "$config" ] || exit 0
jq -e . "$config" >/dev/null 2>&1 || {
    printf 'cratis-quality-gate: %s is not valid JSON — gate skipped.\n' "$config" >&2
    exit 0
}
[ "$(jq -r '.enabled // true' "$config")" = "true" ] || exit 0

# ── What changed in the working tree ─────────────────────────────────────────
git -C "$root" rev-parse --git-dir >/dev/null 2>&1 || exit 0
changed="$(
    {
        git -C "$root" diff --name-only HEAD 2>/dev/null || true
        git -C "$root" ls-files --others --exclude-standard 2>/dev/null || true
    } | LC_ALL=C sort -u
)"
[ -n "$changed" ] || exit 0

gate_count="$(jq -r '.gates | length' "$config")"
[ "${gate_count:-0}" -gt 0 ] || exit 0
fail_fast="$(jq -r '.failFast // true' "$config")"
max_lines="$(jq -r '.maxOutputLines // 60' "$config")"

tmp_root="$(hook_state_dir "$(hook_json "$input" '.session_id')")" || tmp_root="${TMPDIR:-/tmp}"
log_dir="$tmp_root/gate-logs"
mkdir -p "$log_dir" 2>/dev/null || log_dir="${TMPDIR:-/tmp}"

# Does any changed path match this gate's globs (and survive its excludes)?
gate_triggered() {
    local idx="$1" inc exc p
    inc="$(jq -r --argjson i "$idx" '.gates[$i].changed // [] | .[]' "$config")"
    exc="$(jq -r --argjson i "$idx" '.gates[$i].excludeChanged // [] | .[]' "$config")"
    [ -n "$inc" ] || return 1
    while IFS= read -r p; do
        [ -n "$p" ] || continue
        printf '%s\n' "$inc" | hook_glob_match "$p" || continue
        if [ -n "$exc" ]; then
            printf '%s\n' "$exc" | hook_glob_match "$p" && continue
        fi
        return 0
    done <<EOF
$changed
EOF
    return 1
}

# Report the first unmet requirement, or nothing when the gate can run here.
gate_unmet() {
    local idx="$1" c p
    while IFS= read -r c; do
        [ -n "$c" ] || continue
        hook_have "$c" || { printf "command '%s' is not on PATH" "$c"; return 0; }
    done <<EOF
$(jq -r --argjson i "$idx" '.gates[$i].requires.commands // [] | .[]' "$config")
EOF
    while IFS= read -r p; do
        [ -n "$p" ] || continue
        [ -e "$root/$p" ] || { printf "'%s' does not exist in this repository" "$p"; return 0; }
    done <<EOF
$(jq -r --argjson i "$idx" '.gates[$i].requires.paths // [] | .[]' "$config")
EOF
    return 0
}

idx=0
ran=0
while [ "$idx" -lt "$gate_count" ]; do
    id="$(jq -r --argjson i "$idx" '.gates[$i].id' "$config")"
    desc="$(jq -r --argjson i "$idx" '.gates[$i].description // ""' "$config")"
    wd="$(jq -r --argjson i "$idx" '.gates[$i].workingDirectory // "."' "$config")"

    if ! gate_triggered "$idx"; then
        [ "$dryrun" = "1" ] && printf 'cratis-quality-gate: SKIP  %-24s (no matching change)\n' "$id" >&2
        idx=$((idx + 1))
        continue
    fi

    unmet="$(gate_unmet "$idx")"
    if [ -n "$unmet" ]; then
        printf 'cratis-quality-gate: NO-OP %-24s — %s. Configure it in %s.\n' \
            "$id" "$unmet" "${config#"$root"/}" >&2
        idx=$((idx + 1))
        continue
    fi

    cmd=()
    while IFS= read -r arg; do
        cmd+=("$arg")
    done <<EOF
$(jq -r --argjson i "$idx" '.gates[$i].command // [] | .[]' "$config")
EOF
    if [ "${#cmd[@]}" -eq 0 ]; then
        idx=$((idx + 1))
        continue
    fi

    if [ "$dryrun" = "1" ]; then
        printf 'cratis-quality-gate: RUN   %-24s %s\n                            $ %s   (cwd: %s)\n' \
            "$id" "$desc" "${cmd[*]}" "$wd" >&2
        idx=$((idx + 1))
        ran=$((ran + 1))
        continue
    fi

    log="$log_dir/$id.log"
    rc=0
    (cd "$root/$wd" && "${cmd[@]}") >"$log" 2>&1 || rc=$?
    ran=$((ran + 1))

    if [ "$rc" -ne 0 ]; then
        {
            printf 'QUALITY GATE FAILED: %s (exit %s)\n' "$id" "$rc"
            printf '  %s\n' "$desc"
            printf '  $ %s   (cwd: %s)\n\n' "${cmd[*]}" "$wd"
            printf -- '--- last %s lines ---\n' "$max_lines"
            tail -n "$max_lines" "$log" 2>/dev/null || true
            printf -- '--- end ---\n\n'
            printf 'Fix the failure and re-run the gate. Never change code merely to make a gate pass,\n'
            printf 'and never suppress warnings. Full log: %s\n' "$log"
        } >&2
        [ "$fail_fast" = "true" ] && exit 2
        failed=1
    fi

    idx=$((idx + 1))
done

[ "${failed:-0}" -eq 0 ] || exit 2
[ "$dryrun" = "1" ] && printf 'cratis-quality-gate: dry run complete — %s gate(s) would run.\n' "$ran" >&2
exit 0
