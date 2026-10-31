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
hook_have jq || exit 0

# One jq call reads every field the guard uses. Fields are NUL-terminated (embedded NULs are dropped,
# as a command substitution would drop them); a missing, null, false or unreadable path is empty.
cwd="" file="" notebook="" new_content="" new_string=""
{
    IFS= read -r -d '' cwd
    IFS= read -r -d '' file
    IFS= read -r -d '' notebook
    IFS= read -r -d '' new_content
    IFS= read -r -d '' new_string
} < <(jq -j '
    def field(path): ([try (path // empty) catch empty] | first // "")
        | if type == "string" then . else tojson end
        | gsub("\u0000"; "");
    [field(.cwd), field(.tool_input.file_path), field(.tool_input.notebook_path),
     field(.tool_input.content), field(.tool_input.new_string)]
    | .[] | ., "\u0000"
' <<<"$input" 2>/dev/null) || true
# Command substitution stripped trailing newlines from each field; keep that behaviour.
for field_name in cwd file notebook new_content new_string; do
    while [[ "${!field_name}" == *$'\n' ]]; do printf -v "$field_name" '%s' "${!field_name%$'\n'}"; done
done

[ -n "$cwd" ] || cwd="$(hook_repo_root)"
[ -n "$file" ] || file="$notebook"
[ -n "$file" ] || exit 0

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

abspath "$file" "$cwd"
normalize_target_path "$REPLY"
file="$REPLY"
base_of "$file"
base="$REPLY"

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

# ── 4. Agent controls and installed managed trees ─────────────────────────────
agent_control_target() {
    local target="$1" directory policy_root target_rel
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
        case "$target_rel" in
            .cratis/ai/*) return 0 ;;
            .claude/* | .pi/*)
                # The installer records adapter ownership in Integrations (Path, IsDirectory),
                # not Files: those destinations are relative to the managed corpus root.
                # An unreadable/invalid manifest cannot establish ownership of local adapters.
                # Allow unclassified paths; corpus paths and explicit controls still block.
                if jq -e --arg path "$target_rel" '
                    (.Integrations // [])
                    | if type != "array" then false else
                        [.[] | select(type == "object") | select(.Path | type == "string")]
                        | any(.[]; . as $entry | $entry.Path == $path
                            or ($entry.IsDirectory == true and ($path | startswith($entry.Path + "/"))))
                      end
                ' "$policy_root/.cratis/ai.manifest.json" >/dev/null 2>&1; then
                    return 0
                fi
                ;;
        esac
    else
        # The corpus is authored source in Cratis/AI, not a managed installation.
        repository_origin "$policy_root"
        if origin_matches '^(https://github\.com/|git@github\.com:|ssh://git@github\.com(:[0-9]+)?/)cratis/ai(\.git)?/?$'; then
            return 1
        fi
    fi
    case "$target_rel" in
        .claude/settings.json | .claude/settings.local.json | .mcp.json \
            | .pi/settings.json | .pi/extensions/*) return 0 ;;
    esac
    return 1
}

physical_target_path "$file" || physical_file=""
if agent_control_target "$file" || { [ -n "$physical_file" ] && agent_control_target "$physical_file"; }; then
    block \
        "This target controls the agent's permissions or extensions, or belongs to an installed managed corpus/adapter tree. Agent Write/Edit calls may not alter their own controls or managed instructions." \
        "Instead: report the intended change. Use 'cratis ai update' for managed files, or let a person change settings. An explicitly authorized protected write requires CRATIS_HOOKS_ALLOW_PROTECTED_WRITES=1 in the harness environment. Do not retry through a shell write."
fi

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

# The cheap in-shell header test runs first; grep -I then confirms the file is text, as before.
if [ -f "$file" ] \
    && first_lines_carry_marker 2>/dev/null <"$file" \
    && LC_ALL=C grep -Iq . "$file" 2>/dev/null; then
    block \
        "This file is generated output (its header marks it as generated by Cratis). Rule 15 [contract] in .cratis/ai/rules/general.md: never edit a generated file — the next build overwrites it." \
        "Instead: change the C# source it is generated from (the [Command] / [ReadModel] record) and rebuild with 'dotnet build -c Debug' to regenerate the proxy."
fi

# The same guard for authoring a *new* file that pretends to be generated output.
[ -n "$new_content" ] || new_content="$new_string"
if [ -n "$new_content" ] \
    && first_lines_carry_marker <<<"$new_content"; then
    block \
        "The content being written opens with a Cratis generated-file header. Proxies are produced by the Cratis proxy generator, never hand-written." \
        "Instead: write the C# source and run 'dotnet build -c Debug' to generate the proxy."
fi

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
        setting="$(jq -r '.allowDependencyManifestWrites | select(type == "boolean") | tostring' "$policy_root/.cratis/ai.json" 2>/dev/null || true)"
    fi
    case "$setting" in
        true) return 0 ;;
        false) return 1 ;;
    esac
    repository_origin "$policy_root"
    # Anchor host, owner and the single repository segment; never infer ownership from profiles.
    origin_matches '^(https://github\.com/|git@github\.com:|ssh://git@github\.com(:[0-9]+)?/)cratis/[a-z0-9_.-]+/?$'
}

case "$base" in
    Directory.Packages.props | global.json | NuGet.config | NuGet.Config | nuget.config \
        | yarn.lock | package-lock.json | pnpm-lock.yaml | packages.lock.json)
        if manifest_writes_allowed; then exit 0; fi
        block \
            "'$base' is a dependency manifest / lockfile / SDK or NuGet configuration file. .cratis/ai/rules/general.md (Source-of-Truth Discipline): don't change these unless explicitly asked." \
            "To allow dependency updates for this repository, set allowDependencyManifestWrites to true in .cratis/ai.json. If the user explicitly asked for this change, re-run the tool call with CRATIS_HOOKS_ALLOW_PROTECTED_WRITES=1 exported. Otherwise take the dependency as given and solve the problem in source."
        ;;
esac

# ── 3. Environment files ──────────────────────────────────────────────────────
case "$base" in
    .env | .env.* | *.env)
        block \
            "'$base' is an environment file and normally holds secrets. Hooks block writing it so credentials are never authored, rewritten, or echoed by an agent." \
            "Instead: tell the user exactly which variable to set and let them edit the file. If they explicitly asked you to write it, re-run with CRATIS_HOOKS_ALLOW_PROTECTED_WRITES=1 exported."
        ;;
esac

exit 0
