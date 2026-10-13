#!/usr/bin/env bash
# cratis-ai-managed: hooks/scripts/cratis-quality-gate.sh
# Stop hook — the real quality gate.
#
# Looks at what actually changed in the working tree, runs only the gates that change touches,
# and exits 2 (blocking turn-end, stderr fed back to the model) when one fails. It never edits
# code: it only builds, tests and lints. With no changes it exits silently. An empty plan for
# a changed tree reports on stderr that no product verification was performed, without running gates.
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
here="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
config="${CRATIS_HOOKS_GATES:-$here/quality-gates.json}"
dryrun="${CRATIS_HOOKS_GATE_DRYRUN:-0}"

[ -f "$config" ] || exit 0
jq -e . "$config" >/dev/null 2>&1 || {
    printf 'cratis-quality-gate: %s is not valid JSON — gate skipped.\n' "$config" >&2
    exit 0
}

# ── Project-owned overrides ──────────────────────────────────────────────────
# Where a repository's own answer to "which directory does this gate build in" lives. It is
# outside the managed tree on purpose: a repository that instead edits the managed
# quality-gates.json mixes project facts into Cratis-owned content, so the next managed update
# either reports drift or silently discards the repository's own configuration. The override
# states only what differs, keyed by gate id, and nothing here needs a script fork.
#
# The merge is shallow (a patch field replaces the base field), with one exception: a patch that
# replaces `command` but does not state `requires` also drops the base gate's
# requires.packageScripts. Those scripts guard the base command; keeping them would make the
# overriding repository's own command a silent NO-OP whenever it lacks e.g. g:compile.
overrides="$root/.cratis/ai/quality-gates.project.json"
if [ -f "$overrides" ]; then
    if jq -e . "$overrides" >/dev/null 2>&1; then
        merged="$(mktemp "${TMPDIR:-/tmp}/cratis-quality-gates.XXXXXX")"
        if jq -s '
            .[0] as $base | .[1] as $over
            | ($over.gates // []) as $gates
            | def patched($gate; $patch):
                ($gate + ($patch | del(.id))) as $m
                | if ($patch | has("command")) and (($patch | has("requires")) | not) and ($m.requires != null)
                  then $m | .requires |= del(.packageScripts)
                  else $m end;
            $base
            + ($over | del(.gates))
            + { gates: [ $base.gates[] as $gate
                | ($gates | map(select(.id == $gate.id)) | first) as $patch
                | if $patch == null then $gate else patched($gate; $patch) end ] }
        ' "$config" "$overrides" >"$merged" 2>/dev/null; then
            unknown="$(jq -r --slurpfile base "$config" '[.gates // [] | .[].id] - [$base[0].gates[].id] | .[]' "$overrides" 2>/dev/null || true)"
            [ -n "$unknown" ] && printf 'cratis-quality-gate: %s overrides unknown gate(s): %s\n' \
                "${overrides#"$root"/}" "$(printf '%s' "$unknown" | tr '\n' ' ')" >&2
            config="$merged"
        else
            rm -f "$merged"
            printf 'cratis-quality-gate: %s could not be merged — managed gates used unchanged.\n' \
                "${overrides#"$root"/}" >&2
        fi
    else
        printf 'cratis-quality-gate: %s is not valid JSON — managed gates used unchanged.\n' \
            "${overrides#"$root"/}" >&2
    fi
fi

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

# ── Project discovery ────────────────────────────────────────────────────────
# A gate names *what kind of project* it builds, never a product's file. The repository's
# own solution or package is discovered here, so the shipped gates activate unchanged in an
# application, a framework, or a corpus-only repository. Read lazily: a configuration whose
# gates all use literal requires.paths never pays for the listing.
repo_paths=""
repo_paths_read=0
repository_paths() {
    if [ "$repo_paths_read" -eq 0 ]; then
        repo_paths_read=1
        repo_paths="$(
            {
                git -C "$root" ls-files 2>/dev/null || true
                git -C "$root" ls-files --others --exclude-standard 2>/dev/null || true
            } | LC_ALL=C sort -u
        )"
    fi
    printf '%s\n' "$repo_paths"
}

# An install manifest distinguishes a managed copy from the corpus's authored source.
# Resolve actual targets, not adapter names: a product may legitimately own .agents (etc.).
physical_root="$(cd -P "$root" && pwd -P)"
managed_root=""
if [ -f "$root/.cratis/ai.manifest.json" ] && [ -d "$root/.cratis/ai" ]; then
    managed_root="$(cd -P "$root/.cratis/ai" && pwd -P)"
fi

repository_contains_path() {
    case "$1" in
        "$physical_root"|"$physical_root"/*) ;;
        *) return 1 ;;
    esac
    if [ -n "$managed_root" ]; then
        case "$1" in
            "$managed_root"|"$managed_root"/*) return 1 ;;
        esac
    fi
}

repository_execution_directory() {
    local dir
    dir="$(cd -P "$root/$1" 2>/dev/null && pwd -P)" || return 1
    repository_contains_path "$dir"
}

repository_project_path() {
    local path="$root/$1" parent dir target hops=0
    [ -f "$path" ] || return 1
    parent=${1%/*}
    [ "$parent" != "$1" ] || parent=.
    # The execution directory must be owned too, even if the file links back into the repo.
    repository_execution_directory "$parent" || return 1
    while :; do
        dir="$(cd -P "${path%/*}" 2>/dev/null && pwd -P)" || return 1
        path="$dir/${path##*/}"
        [ -L "$path" ] || break
        hops=$((hops + 1))
        [ "$hops" -le 40 ] || return 1
        target="$(readlink "$path")" || return 1
        case "$target" in
            /*) path="$target" ;;
            *) path="$dir/$target" ;;
        esac
    done
    repository_contains_path "$path"
}

# Indexed arrays work on Bash 3.2. Populate caches in the parent shell, not a command
# substitution: identical frontend gates share one listing and one physical resolution.
project_cache_paths=()
project_cache_valid=()
candidate_cache_globs=()
candidate_cache_dirs=()
candidate_dirs=""
prepare_candidates() {
    local globs="$1" i group selected_group="" candidate dir candidates valid
    for ((i=0; i<${#candidate_cache_globs[@]}; i++)); do
        if [ "${candidate_cache_globs[$i]}" = "$globs" ]; then
            candidate_dirs="${candidate_cache_dirs[$i]}"
            return 0
        fi
    done
    repository_paths >/dev/null
    # One pass matches all globs; the first group with an eligible candidate retains priority.
    candidates="$(printf '%s\n' "$repo_paths" | CRATIS_GLOBS="$globs" awk "$hook_glob_awk_lib"'
        BEGIN {
            count = split(ENVIRON["CRATIS_GLOBS"], globs, "\n")
            for (i = 1; i <= count; i++) res[i] = g2re(globs[i])
        }
        { paths[++n] = $0 }
        END {
            for (i = 1; i <= count; i++) for (j = 1; j <= n; j++)
                if (!seen[paths[j]] && paths[j] ~ res[i]) { print i "\t" paths[j]; seen[paths[j]] = 1 }
        }
    ')"
    candidate_dirs=""
    while IFS=$'\t' read -r group candidate; do
        [ -n "$candidate" ] || continue
        if [ -n "$selected_group" ] && [ "$group" != "$selected_group" ]; then
            break
        fi
        valid=""
        for ((i=0; i<${#project_cache_paths[@]}; i++)); do
            if [ "${project_cache_paths[$i]}" = "$candidate" ]; then
                valid="${project_cache_valid[$i]}"
                break
            fi
        done
        if [ -z "$valid" ]; then
            valid=0
            repository_project_path "$candidate" && valid=1
            project_cache_paths+=("$candidate")
            project_cache_valid+=("$valid")
        fi
        [ "$valid" = 1 ] || continue
        selected_group="$group"
        dir=${candidate%/*}
        [ "$dir" != "$candidate" ] || dir=.
        candidate_dirs="${candidate_dirs}${candidate_dirs:+
}$dir"
        # Keep main's root workspace/solution dispatch; it owns dependent-workspace coverage.
        [ "$dir" != "." ] || { candidate_dirs="."; break; }
    done <<EOF
$candidates
EOF
    candidate_cache_globs+=("$globs")
    candidate_cache_dirs+=("$candidate_dirs")
}

# With no eligible root project, select the deepest containing directory per trigger.
# Prefix lookup is one awk pass, not a subprocess for every path/candidate pair.
discover_workdirs() {
    [ -n "$candidate_dirs" ] || return 1
    [ "$candidate_dirs" != "." ] || { printf '.\n'; return 0; }
    printf '%s\n' "$1" | CRATIS_PROJECT_DIRS="$candidate_dirs" awk '
        BEGIN {
            n = split(ENVIRON["CRATIS_PROJECT_DIRS"], dirs, "\n")
            for (i = 1; i <= n; i++) if (!known[dirs[i]]++) ordered[++count] = dirs[i]
        }
        NF {
            parent = $0; found = 0
            while (sub("/[^/]*$", "", parent)) {
                if (known[parent]) { selected[parent] = 1; found = 1; break }
            }
            if (!found) {
                if (count == 1) selected[ordered[1]] = 1
                else {
                    printf "cratis-quality-gate: UNVERIFIED — no containing project for %s among multiple candidates. Configure workingDirectory.\n", $0 > "/dev/stderr"
                    failed = 1
                }
            }
        }
        END {
            if (failed) exit 2
            for (i = 1; i <= count; i++) if (selected[ordered[i]]) print ordered[i]
        }
    '
}

gate_count="$(jq -r '.gates | length' "$config")"
[ "${gate_count:-0}" -gt 0 ] || exit 0
fail_fast="$(jq -r '.failFast // true' "$config")"
max_lines="$(jq -r '.maxOutputLines // 60' "$config")"

tmp_root="$(hook_state_dir "$(hook_json "$input" '.session_id')")" || tmp_root="${TMPDIR:-/tmp}"
log_dir="$tmp_root/gate-logs"
mkdir -p "$log_dir" 2>/dev/null || log_dir="${TMPDIR:-/tmp}"

# Collect ALL paths matching this gate's globs after its exclusions, for discovery too.
gate_changed_paths() {
    local idx="$1" inc exc
    inc="$(jq -r --argjson i "$idx" '.gates[$i].changed // [] | .[]' "$config")"
    exc="$(jq -r --argjson i "$idx" '.gates[$i].excludeChanged // [] | .[]' "$config")"
    printf '%s\n' "$changed" | CRATIS_INCLUDES="$inc" CRATIS_EXCLUDES="$exc" awk "$hook_glob_awk_lib"'
        BEGIN {
            ni = split(ENVIRON["CRATIS_INCLUDES"], includes, "\n")
            ne = split(ENVIRON["CRATIS_EXCLUDES"], excludes, "\n")
            for (i = 1; i <= ni; i++) if (includes[i] != "") inc[++ic] = g2re(includes[i])
            for (i = 1; i <= ne; i++) if (excludes[i] != "") exc[++ec] = g2re(excludes[i])
        }
        NF {
            matches = 0
            for (i = 1; i <= ic; i++) if ($0 ~ inc[i]) { matches = 1; break }
            if (!matches) next
            for (i = 1; i <= ec; i++) if ($0 ~ exc[i]) { matches = 0; break }
            if (matches) print
        }
    '
}

# Does the package.json at $1 define the script named $2? A missing file or a file that is not
# valid JSON defines nothing.
package_defines_script() {
    [ -f "$1" ] && jq -e --arg s "$2" '.scripts[$s] != null' "$1" >/dev/null 2>&1
}

# Requirements are read once per gate, not once per affected directory.
# Report the first unmet requirement for the repo-relative execution directory $1.
gate_unmet() {
    local dir="$1" c p s
    while IFS= read -r c; do
        [ -n "$c" ] || continue
        hook_have "$c" || { printf "command '%s' is not on PATH" "$c"; return 0; }
    done <<EOF
$required_commands
EOF
    while IFS= read -r p; do
        [ -n "$p" ] || continue
        [ -e "$root/$p" ] || { printf "'%s' does not exist in this repository" "$p"; return 0; }
    done <<EOF
$required_paths
EOF
    # A package.json script a gate invokes must exist, or yarn fails with "Couldn't find a script"
    # instead of the gate being a NO-OP. A global script (g:) may live in the root package.json
    # that the workspace's own package.json defers to. That fallback assumes Yarn Berry, where
    # a g: script defined at the root is runnable from every workspace; no workspace package.json
    # is inspected.
    while IFS= read -r s; do
        [ -n "$s" ] || continue
        package_defines_script "$root/$dir/package.json" "$s" && continue
        case "$s" in
            g:*) package_defines_script "$root/package.json" "$s" && continue ;;
        esac
        printf "script '%s' is not defined in %s" "$s" "$dir/package.json"
        return 0
    done <<EOF
$required_scripts
EOF
    return 0
}

idx=0
ran=0
planned_gates=0
planned_directories=()
while [ "$idx" -lt "$gate_count" ]; do
    id="$(jq -r --argjson i "$idx" '.gates[$i].id' "$config")"
    desc="$(jq -r --argjson i "$idx" '.gates[$i].description // ""' "$config")"
    wd="$(jq -r --argjson i "$idx" '.gates[$i].workingDirectory // ""' "$config")"

    gate_changes="$(gate_changed_paths "$idx")"
    if [ -z "$gate_changes" ]; then
        [ "$dryrun" = "1" ] && printf 'cratis-quality-gate: SKIP  %-24s (no matching change)\n' "$id" >&2
        idx=$((idx + 1))
        continue
    fi

    # Discovery is a requirement and can select multiple affected project directories.
    workdirs="$wd"
    if [ -z "$workdirs" ]; then
        wd_globs="$(jq -r --argjson i "$idx" '.gates[$i].workingDirectoryFrom // [] | .[]' "$config")"
        if [ -n "$wd_globs" ]; then
            prepare_candidates "$wd_globs"
            discovery_rc=0
            workdirs="$(discover_workdirs "$gate_changes")" || discovery_rc=$?
            if [ "$discovery_rc" -eq 2 ]; then
                printf 'cratis-quality-gate: UNVERIFIED %-24s — ambiguous project discovery.\n' "$id" >&2
                exit 2
            fi
            if [ -z "$workdirs" ]; then
                printf 'cratis-quality-gate: NO-OP %-24s — no repository path matches %s. Configure it in %s.\n' \
                    "$id" "$(printf '%s' "$wd_globs" | tr '\n' ' ')" "${config#"$root"/}" >&2
                idx=$((idx + 1))
                continue
            fi
        fi
    fi
    [ -n "$workdirs" ] || workdirs="."

    required_commands="$(jq -r --argjson i "$idx" '.gates[$i].requires.commands // [] | .[]' "$config")"
    required_paths="$(jq -r --argjson i "$idx" '.gates[$i].requires.paths // [] | .[]' "$config")"
    required_scripts="$(jq -r --argjson i "$idx" '.gates[$i].requires.packageScripts // [] | .[]' "$config")"
    cmd=()
    while IFS= read -r arg; do
        cmd+=("$arg")
    done <<EOF
$(jq -r --argjson i "$idx" '.gates[$i].command // [] | .[]' "$config")
EOF

    project_index=0
    gate_ran=0
    gate_noops=0
    while IFS= read -r wd; do
        project_index=$((project_index + 1))
        if ! repository_execution_directory "$wd"; then
            printf 'cratis-quality-gate: UNVERIFIED %-24s — execution directory %s is outside the repository or inside the managed corpus.\n' "$id" "$wd" >&2
            exit 2
        fi
        unmet="$(gate_unmet "$wd")"
        if [ -n "$unmet" ]; then
            printf 'cratis-quality-gate: NO-OP %-24s — %s. Configure it in %s.\n' \
                "$id" "$unmet" "${config#"$root"/}" >&2
            gate_noops=$((gate_noops + 1))
            continue
        fi
        if [ "${#cmd[@]}" -eq 0 ]; then
            continue
        fi

        gate_ran=$((gate_ran + 1))
        if [ "$dryrun" = "1" ]; then
            [ "$gate_ran" -ne 1 ] || planned_gates=$((planned_gates + 1))
            directory_seen=0
            if [ "${#planned_directories[@]}" -gt 0 ]; then
                for directory in "${planned_directories[@]}"; do
                    [ "$directory" != "$wd" ] || directory_seen=1
                done
            fi
            [ "$directory_seen" -ne 0 ] || planned_directories+=("$wd")
            printf 'cratis-quality-gate: RUN   %-24s %s\n                            $ %s   (cwd: %s)\n' \
                "$id" "$desc" "${cmd[*]}" "$wd" >&2
            ran=$((ran + 1))
            continue
        fi

        log="$log_dir/$id.log"
        [ "$project_index" -eq 1 ] || log="$log_dir/$id-$project_index.log"
        rc=0
        {
            printf 'cratis-quality-gate: LOG %s (cwd: %s)\n' "$id" "$wd"
            (cd -P "$root/$wd" && "${cmd[@]}") </dev/null
        } >"$log" 2>&1 || rc=$?
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
    done <<EOF
$workdirs
EOF
    if [ "$gate_ran" -gt 0 ] && [ "$gate_noops" -gt 0 ]; then
        printf 'cratis-quality-gate: UNVERIFIED %-24s — %s affected project(s) did not meet requirements; the gate is incomplete.\n' "$id" "$gate_noops" >&2
        [ "$fail_fast" = "true" ] && exit 2
        failed=1
    fi
    idx=$((idx + 1))
done

[ "${failed:-0}" -eq 0 ] || exit 2
[ "$dryrun" = "1" ] && printf 'cratis-quality-gate: dry run complete — %s gate(s) would run across %s project directory(s) (%s run(s)).\n' \
    "$planned_gates" "${#planned_directories[@]}" "$ran" >&2
[ "$ran" -eq 0 ] && printf 'cratis-quality-gate: no gates selected — product verification was not performed.\n' >&2
exit 0
