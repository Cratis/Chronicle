#!/usr/bin/env bash
# cratis-ai-managed: hooks/scripts/cratis-guard-writes.sh
# PreToolUse hook — hard block on writes that must never happen.
#
# Exits 2 (block the tool call, stderr goes back to the model) for:
#   1. Generated files      — anything whose header marks it as Cratis-generated output
#                             (.cratis/ai/rules/general.md rule 15 [contract])
#   2. Dependency manifests — Directory.Packages.props, global.json, lockfiles, NuGet config
#                             (allowed by default for a github.com/Cratis origin)
#   3. Environment files    — .env and friends (secrets)
#   4. Agent controls       — permissions, hooks and installed managed corpus/adapters
#
# While jq is missing the guard cannot read the tool call, so it refuses every call.
#
# .cratis/ai.json allowDependencyManifestWrites overrides only the manifest policy:
# true allows them in any repository; false blocks them even in a Cratis repository.
#
# Escape hatch for the "unless explicitly asked" case — the user asks, you set it for the call:
#   CRATIS_HOOKS_ALLOW_PROTECTED_WRITES=1
set -euo pipefail

# SCRIPTDIR, not a path relative to the caller: shellcheck resolves a plain relative `source=`
# against the current working directory, and these hooks are linted from wherever CI happens to run.
script_directory="${BASH_SOURCE[0]%/*}"
[ "$script_directory" != "${BASH_SOURCE[0]}" ] || script_directory=.
# shellcheck source=SCRIPTDIR/hook-lib.sh
. "$script_directory/hook-lib.sh"

[ "${CRATIS_HOOKS_ALLOW_PROTECTED_WRITES:-0}" = "1" ] && exit 0

# This guard runs before every write, so it avoids subprocesses where a builtin does the same job:
# the helpers below return through REPLY instead of printing into a command substitution.
# Read stdin with the read builtin: opening /dev/stdin fails with ENXIO when stdin is a socket (Linux, Node pipes).
input=""
IFS= read -r -d '' input || true
[ -n "$input" ] || exit 0
hook_require_jq cratis-guard-writes CRATIS_HOOKS_ALLOW_PROTECTED_WRITES

parser_failed() {
    printf '%s\n' 'BLOCKED by cratis-guard-writes: parser failed; cannot check this tool call.' >&2
    exit 2
}

# One jq call reads every field the guard uses. Fields are NUL-terminated (embedded NULs are dropped,
# as a command substitution would drop them); a missing, null or false path is empty.
# The process substitution's exit status does not reach the reading group, and Bash 3.2 (macOS /bin/bash)
# cannot wait on its PID, so a sentinel field is written only after jq succeeds.
reference_mode=0
[ "${1:-}" != --references ] || reference_mode=1
cwd="" file="" notebook="" new_content="" new_string="" references="" parser_status=""
{
    IFS= read -r -d '' cwd
    IFS= read -r -d '' file
    IFS= read -r -d '' notebook
    IFS= read -r -d '' new_content
    IFS= read -r -d '' new_string
    IFS= read -r -d '' references
    IFS= read -r -d '' parser_status
} < <(jq -j -s '
    def field(path): ([try (path // empty) catch empty] | first // "")
        | if type == "string" then . else tojson end
        | gsub("\u0000"; "");
    # Slurping parses the whole input before any output, and exactly one document is accepted,
    # so no field from a later document can stand in for the sentinel.
    if length == 1 then .[0] else error("expected one JSON document") end
    | [field(.cwd), field(.tool_input.file_path), field(.tool_input.notebook_path),
     field(.tool_input.content), field(.tool_input.new_string), (field(.tool_input.file_paths) | if . == "" then "[]" else . end)]
    | .[] | ., "\u0000"
' <<<"$input" 2>/dev/null && printf 'ok\0') || parser_failed
[ "$parser_status" = "ok" ] || parser_failed
# Command substitution stripped trailing newlines from each field; keep that behaviour.
for field_name in cwd file notebook new_content new_string; do
    while [[ "${!field_name}" == *$'\n' ]]; do printf -v "$field_name" '%s' "${!field_name%$'\n'}"; done
done

[ -n "$cwd" ] || cwd="$(hook_repo_root)"
[ -n "$file" ] || file="$notebook"
[ -n "$file" ] || [ "$reference_mode" = 1 ] || exit 0

# dir_of / base_of / relative_to mirror dirname, basename and hook_relpath for the absolute,
# normalized paths used here. abspath mirrors hook_abspath. All return through REPLY.
dir_of() {
    REPLY="${1%/*}"
    [ -n "$REPLY" ] || REPLY=/
}

base_of() {
    REPLY="${1##*/}"
    [ -n "$REPLY" ] || REPLY=/
}

abspath() {
    case "$1" in
        /*) REPLY="$1" ;;
        *) REPLY="${2%/}/$1" ;;
    esac
}

relative_to() {
    local root="${2%/}"
    case "$1" in
        "$root"/*) REPLY="${1#"$root"/}" ;;
        *) REPLY="$1" ;;
    esac
}

# Lexically normalize before any Git lookup or parent walk, including nonexistent segments.
# Component parsing preserves spaces and glob characters without depending on realpath.
normalize_target_path() {
    local remaining="$1" normalized="" component
    while [ -n "$remaining" ]; do
        component="${remaining%%/*}"
        if [ "$remaining" = "$component" ]; then
            remaining=""
        else
            remaining="${remaining#*/}"
        fi
        case "$component" in
            "" | .) ;;
            ..) normalized="${normalized%/*}" ;;
            *) normalized="$normalized/$component" ;;
        esac
    done
    REPLY="${normalized:-/}"
}

block() {
    # Pi can run the bundled script in a consumer checkout; this root is only for diagnostics,
    # so it is looked up only when a write is actually refused.
    local root rel
    root="$(git -C "$cwd" rev-parse --show-toplevel 2>/dev/null || printf '%s\n' "$cwd")"
    relative_to "$file" "$root"
    rel="$REPLY"
    printf 'BLOCKED by cratis-guard-writes: %s\n\n%s\n\n%s\n' "$rel" "$1" "$2" >&2
    exit 2
}

# Check both the addressed path and its physical destination: settings may be symlinks to
# templates, and an innocently named link may target a protected file. Preserve missing suffixes.
physical_target_path() {
    local target="$1" directory suffix="" link hops=0 start="$PWD"
    physical_file=""
    while [ -L "$target" ]; do
        hops=$((hops + 1))
        [ "$hops" -le 40 ] || return 1
        link="$(readlink "$target")" || return 1
        dir_of "$target"
        abspath "$link" "$REPLY"
        target="$REPLY"
    done
    dir_of "$target"
    directory="$REPLY"
    base_of "$target"
    suffix="/$REPLY"
    while [ ! -d "$directory" ]; do
        [ "$directory" != "/" ] || return 1
        base_of "$directory"
        suffix="/$REPLY$suffix"
        dir_of "$directory"
        directory="$REPLY"
    done
    cd -P "$directory" 2>/dev/null || return 1
    directory="$PWD"
    cd "$start" 2>/dev/null || true
    physical_file="${directory%/}$suffix"
}

# The origin URL, looked up once per repository per run.
origin_cache_root=""
origin_cache_value=""
repository_origin() {
    if [ "$origin_cache_root" != "$1" ]; then
        origin_cache_value="$(git -C "$1" remote get-url origin 2>/dev/null || true)"
        origin_cache_root="$1"
    fi
    REPLY="$origin_cache_value"
}

# Case-insensitive ASCII match of the origin URL, equivalent to grep -Eiq on a single line.
origin_matches() {
    # C locale, as the grep it replaced ran: a non-ASCII letter (the Kelvin sign) must not match [a-z].
    local LC_ALL=C pattern="$1" matched=1
    shopt -s nocasematch
    # shellcheck disable=SC2034
    [[ "$REPLY" =~ $pattern ]] && matched=0
    shopt -u nocasematch
    return "$matched"
}

# One policy vocabulary for normal Write/Edit and whole-command reference checks.
manifest_patterns=(package.json Directory.Packages.props global.json NuGet.config yarn.lock package-lock.json pnpm-lock.yaml packages.lock.json)
environment_patterns=('.env' '.env.*' '*.env')
control_patterns=('.claude/settings.json' '.claude/settings.local.json' '.mcp.json' '.pi/settings.json' '.pi/extensions/*')

# Glob-language intersection, not expansion/eval. A star has an epsilon transition and
# a character loop. The product automaton accepts iff the two patterns share a spelling.
# Braces conservatively become a star; bracket classes retain their ordinary meaning.
IFS= read -r -d '' glob_program <<'AWK' || true
function tokens(s, side,    i, c, j, t, n) {
    s = tolower(s)
    for (i = 1; i <= length(s); i++) {
        c = substr(s, i, 1); t = c
        if (c == "{") { j = index(substr(s, i), "}"); if (j) i += j - 1; t = "*" }
        else if (c == "[") {
            j = index(substr(s, i + 1), "]")
            if (j) { t = substr(s, i, j + 1); i += j; sub(/^\[!/, "[^", t) }
        }
        N[side]++; A[side, N[side]] = t
    }
}
function push(i, j,    key) { key = i SUBSEP j; if (!(key in seen)) { seen[key] = 1; L[++last] = i; R[last] = j } }
function accepts(t, c, side) {
    if (side == 1 && c == "/" && (t == "*" || t == "?" || length(t) > 1)) return 0
    return t == "*" || t == "?" || (length(t) > 1 && substr(t, 1, 1) == "[" ? c ~ ("^" t "$") : t == c)
}
function compatible(a, b,    k, c) {
    if (length(a) == 1 && a != "*" && a != "?") return accepts(b, a, 2)
    if (length(b) == 1 && b != "*" && b != "?") return accepts(a, b, 1)
    for (k = 1; k < 256; k++) { c = sprintf("%c", k); if (accepts(a, c, 1) && accepts(b, c, 2)) return 1 }
    return 0
}
BEGIN {
    tokens(ENVIRON["REF_LEFT"], 1); tokens(ENVIRON["REF_RIGHT"], 2); push(1, 1)
    while (next_state < last) {
        if (last > 65536) exit 2
        i = L[++next_state]; j = R[next_state]
        if (i > N[1] && j > N[2]) exit 0
        a = A[1, i]; b = A[2, j]
        if (a == "*") push(i + 1, j)
        if (b == "*") push(i, j + 1)
        if (i <= N[1] && j <= N[2] && compatible(a, b)) push(i + (a != "*"), j + (b != "*"))
    }
    exit 1
}
AWK

names_match() {
    local value="$1" pattern status LC_ALL=C
    shift
    for pattern in "$@"; do
        if [ "$reference_pattern" = 1 ]; then
            if REF_LEFT="$value" REF_RIGHT="$pattern" LC_ALL=C awk "$glob_program" </dev/null; then return 0
            else status=$?; [ "$status" = 1 ] || parser_failed; fi
        else
            shopt -s nocasematch
            # Intentional policy glob matching, not literal filename equality.
            # shellcheck disable=SC2053
            if [[ "$value" == $pattern ]]; then shopt -u nocasematch; return 0; fi
            shopt -u nocasematch
        fi
    done
    return 1
}

# Directory references include owned descendants, without classifying every ordinary
# directory as a possible environment file. Normal Write/Edit classification is unchanged.
control_match() {
    local value="$1" reference_pattern="$reference_pattern" pattern LC_ALL=C
    shift
    if names_match "$value" "$@"; then return 0; fi
    [ "$reference_mode" = 1 ] || return 1
    if [ "$reference_pattern" = 0 ]; then
        shopt -s nocasematch
        for pattern in "$@"; do
            if [[ "$pattern" == "${value%/}/"* ]]; then shopt -u nocasematch; return 0; fi
        done
        shopt -u nocasematch
        return 1
    fi
    names_match "${value%/}/*" "$@"
}

# ── 4. Agent controls and installed managed trees ─────────────────────────────
agent_control_target() {
    local target="$1" directory policy_root target_rel managed pattern
    dir_of "$target"
    directory="$REPLY"
    # Keep the addressed spelling here, even when an adapter directory is a symlink.
    # .git is a directory in a checkout and a file in a linked worktree.
    while [ ! -e "$directory/.git" ]; do
        [ "$directory" != "/" ] || return 1
        dir_of "$directory"
        directory="$REPLY"
    done
    policy_root="$directory"
    relative_to "$target" "$policy_root"
    target_rel="$REPLY"
    if [ -f "$policy_root/.cratis/ai.manifest.json" ]; then
        if control_match "$target_rel" '.cratis/ai/*'; then return 0; fi
        if control_match "$target_rel" '.claude/*' '.pi/*'; then
            # Integration paths, not corpus Files, establish adapter ownership.
            managed="$(jq -r '
                (.Integrations // []) | if type != "array" then empty else
                    .[] | select(type == "object") | select(.Path | type == "string")
                    | if (.Path | explode | any(.[]; . == 0 or . == 9 or . == 10 or . == 13)) then error("invalid integration path") else
                        .Path, (if .IsDirectory == true then .Path + "/*" else empty end) end
                  end
            ' "$policy_root/.cratis/ai.manifest.json" 2>/dev/null)" || parser_failed
            while IFS= read -r pattern; do
                [ -n "$pattern" ] || continue
                if control_match "$target_rel" "$pattern"; then return 0; fi
            done <<<"$managed"
        fi
    else
        # The corpus is authored source in Cratis/AI, not a managed installation.
        repository_origin "$policy_root"
        if origin_matches '^(https://github\.com/|git@github\.com:|ssh://git@github\.com(:[0-9]+)?/)cratis/ai(\.git)?/?$'; then
            return 1
        fi
    fi
    control_match "$target_rel" "${control_patterns[@]}"
}

# ── 1. Generated files ────────────────────────────────────────────────────────
# The marker must be a real header: a comment opener at the start of one of the first few lines.
# Merely *mentioning* the string — documentation, a rule file, this corpus — is not a match.
marker='^[[:space:]]*(//|/\*|#|<!--)[[:space:]]*@generated by Cratis'

# True when one of the first five lines of stdin carries the marker (head -n 5 | grep -E, without forks).
first_lines_carry_marker() {
    local LC_ALL=C line count=0
    while [ "$count" -lt 5 ] && { IFS= read -r line || [ -n "$line" ]; }; do
        count=$((count + 1))
        # shellcheck disable=SC2034
        [[ "$line" =~ $marker ]] && return 0
    done
    return 1
}

# ── 2. Dependency manifests, lockfiles, SDK and NuGet configuration ───────────
manifest_writes_allowed() {
    local setting="" directory policy_root start="$PWD"
    # Authorization belongs to the write target, which may be outside the session's checkout.
    # New files can have nonexistent parents; find their nearest existing directory first.
    dir_of "$file"
    directory="$REPLY"
    while [ ! -d "$directory" ]; do
        [ "$directory" != "/" ] || return 1
        dir_of "$directory"
        directory="$REPLY"
    done
    # Follow existing directory symlinks before asking Git which repository owns the target.
    cd -P "$directory" 2>/dev/null || return 1
    directory="$PWD"
    cd "$start" 2>/dev/null || true
    policy_root="$(git -C "$directory" rev-parse --show-toplevel 2>/dev/null)" || return 1
    if [ -f "$policy_root/.cratis/ai.json" ]; then
        # Only actual booleans override the default; jq's `// empty` would discard false.
        setting="$(jq -r '.allowDependencyManifestWrites | select(type == "boolean") | tostring' "$policy_root/.cratis/ai.json" 2>/dev/null)" || parser_failed
    fi
    case "$setting" in
        true) return 0 ;;
        false) return 1 ;;
    esac
    repository_origin "$policy_root"
    # Anchor host, owner and the single repository segment; never infer ownership from profiles.
    origin_matches '^(https://github\.com/|git@github\.com:|ssh://git@github\.com(:[0-9]+)?/)cratis/[a-z0-9_.-]+/?$'
}

# Return the category, leaving normal Write/Edit diagnostics unchanged below.
classify_file() {
    local reference_pattern="${2:-0}"
    abspath "$1" "$cwd"; normalize_target_path "$REPLY"; file="$REPLY"
    base_of "$file"; base="$REPLY"
    physical_target_path "$file" || physical_file=""
    if agent_control_target "$file" || { [ -n "$physical_file" ] && agent_control_target "$physical_file"; }; then
        classification=controls; return 0
    fi
    if [ -f "$file" ] && first_lines_carry_marker 2>/dev/null <"$file" && LC_ALL=C grep -Iq . "$file" 2>/dev/null; then
        classification=generated; return 0
    fi
    if [ -n "$new_content" ] && first_lines_carry_marker <<<"$new_content"; then classification=content; return 0; fi
    if names_match "$base" "${manifest_patterns[@]}"; then
        if manifest_writes_allowed; then return 1; fi
        classification=manifest; return 0
    fi
    if names_match "$base" "${environment_patterns[@]}"; then classification=environment; return 0; fi
    return 1
}

if [ "$reference_mode" = 1 ]; then
    candidates=() parser_status=""
    while IFS= read -r -d '' candidate; do candidates[${#candidates[@]}]="$candidate"; done < <(
        jq -j 'if type == "array" and length <= 256 and all(.[]; type == "string" and (explode | all(.[]; . != 0 and . != 9 and . != 10 and . != 13)))
            then .[], "ok" else error("invalid reference candidates") end | ., "\u0000"' <<<"$references" 2>/dev/null
    )
    [ "${#candidates[@]}" -gt 0 ] || parser_failed
    last=$((${#candidates[@]} - 1))
    [ "${candidates[$last]}" = ok ] || parser_failed
    [ "$last" -gt 0 ] || exit 0
    unset 'candidates[last]'
    new_content=""
    for candidate in "${candidates[@]}"; do
        reference_pattern=0
        case "$candidate" in *'*'* | *'?'* | *'['* | *'{'*) reference_pattern=1 ;; esac
        if classify_file "$candidate" "$reference_pattern"; then printf '%s\n' "$candidate"; continue; fi
        # Existing generated files can have arbitrary names; inspect actual glob matches too.
        if [ "$reference_pattern" = 1 ]; then
            count=0
            while IFS= read -r expanded; do
                count=$((count + 1)); [ "$count" -le 64 ] || parser_failed
                if classify_file "$expanded"; then printf '%s\n' "$candidate"; break; fi
            done < <(abspath "$candidate" "$cwd"; compgen -G "$REPLY" || true)
        fi
    done
    exit 0
fi

[ -n "$new_content" ] || new_content="$new_string"
if ! classify_file "$file"; then exit 0; fi
case "$classification" in
    controls) block \
        "This target controls the agent's permissions or extensions, or belongs to an installed managed corpus/adapter tree. Agent Write/Edit calls may not alter their own controls or managed instructions." \
        "Instead: report the intended change. Use 'cratis ai update' for managed files, or let a person change settings. An explicitly authorized protected write requires CRATIS_HOOKS_ALLOW_PROTECTED_WRITES=1 in the harness environment. Do not retry through a shell write." ;;
    generated) block \
        "This file is generated output (its header marks it as generated by Cratis). Rule 15 [contract] in .cratis/ai/rules/general.md: never edit a generated file — the next build overwrites it." \
        "Instead: change the C# source it is generated from (the [Command] / [ReadModel] record) and rebuild with 'dotnet build -c Debug' to regenerate the proxy." ;;
    content) block \
        "The content being written opens with a Cratis generated-file header. Proxies are produced by the Cratis proxy generator, never hand-written." \
        "Instead: write the C# source and run 'dotnet build -c Debug' to generate the proxy." ;;
    manifest) block \
        "'$base' is a dependency manifest / lockfile / SDK or NuGet configuration file. .cratis/ai/rules/general.md (Source-of-Truth Discipline): don't change these unless explicitly asked." \
        "To allow dependency updates for this repository, set allowDependencyManifestWrites to true in .cratis/ai.json. If the user explicitly asked for this change, re-run the tool call with CRATIS_HOOKS_ALLOW_PROTECTED_WRITES=1 exported. Otherwise take the dependency as given and solve the problem in source." ;;
    environment) block \
        "'$base' is an environment file and normally holds secrets. Hooks block writing it so credentials are never authored, rewritten, or echoed by an agent." \
        "Instead: tell the user exactly which variable to set and let them edit the file. If they explicitly asked you to write it, re-run with CRATIS_HOOKS_ALLOW_PROTECTED_WRITES=1 exported." ;;
esac
