#!/usr/bin/env bash
# Resolves every `@cratis/<package>/<subpath>` the AI corpus references against the `exports` map of
# the package actually installed under node_modules, and WARNS about each one that cannot resolve.
#
# WARN, never fail — deliberately. The exports map is exact, but the *conclusion* drawn from a miss
# is not: "the corpus documents an API that does not exist" and "this repository is pinned behind the
# version the corpus documents" produce the identical observation. This script is propagated to every
# Cratis repository and runs in the `ai-corpus` CI job, which checks out the tree and installs
# nothing — so a fatal verdict would either be a permanent no-op there or turn a repo red for its own
# dependency pin. A warning is the honest signal: look at this line, decide which of the two it is.
#
# Not installed is not a finding. A missing node_modules, a missing @cratis scope, a package this
# repository does not depend on, and a package published without an `exports` map are all skipped
# silently — there is nothing authoritative to compare against.
#
# Portable: bash 3.2 + grep + sed, with `jq` as the one accepted dependency (absent -> silent no-op,
# per the hook design constraints in ../README.md).
#
# Usage: validate-package-subpaths.sh [root ...]      # default roots: .ai/rules .ai/skills .ai/agents .ai/prompts
#        CRATIS_HOOKS_SUBPATH_REPORT=1 ...            # also print every reference and its resolved status
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
cd "$root"

warn() { printf 'ai-corpus warn: %s\n' "$1" >&2; }
report() { [[ "${CRATIS_HOOKS_SUBPATH_REPORT:-0}" == "1" ]] && printf 'ai-corpus subpath: %s\n' "$1" >&2 || true; }

# Tier 3 over the same roots: whether the .NET types the corpus names in prose and in C# positions
# exist at all. Invoked exactly like Tier 2 at the bottom of this file — tested with -f, not -x, and
# run through `bash`, so a checkout that lost the exec bit does not silently drop the guard — but
# necessarily *above* the two gates below, because it reads a NuGet cache rather than node_modules
# and must still run in a repository that has no frontend and no `jq`.
types="$(dirname "${BASH_SOURCE[0]}")/validate-type-references.sh"
if [[ -f "$types" ]]; then bash "$types" "$@" || true; fi

command -v jq >/dev/null 2>&1 || exit 0
[[ -d node_modules/@cratis ]] || exit 0

# `.ai/hooks` is deliberately not a default root: this file and ../README.md name deliberately-bogus
# subpaths as examples, and a guard that reports its own documentation is a guard people switch off.
if [[ $# -gt 0 ]]; then roots=("$@"); else roots=(.ai/rules .ai/skills .ai/agents .ai/prompts); fi
scan=()
for d in "${roots[@]}"; do [[ -d "$d" ]] && scan+=("$d"); done
[[ "${#scan[@]}" -gt 0 ]] || exit 0

# A line that carries a version alongside the reference has already declared the skew on purpose
# (`(**≥ 3.0.0**)`), so it is not drift. Kept deliberately generous — a dotted version, an `N.x`, or
# either inequality spelling qualifies — because the cost of a missed warning is one stale line while
# the cost of a false one is noise on exactly the lines someone just fixed correctly.
version_re='[0-9]+\.[0-9x]+|≥|>='

# `node_modules/@cratis/...` in the corpus is a filesystem path (a "look in the .d.ts" pointer), not a
# module specifier. Capture the prefix so it can be dropped rather than mis-parsed as a subpath.
refs="$(grep -rhoE '(node_modules/)?@cratis/[A-Za-z0-9._-]+(/[A-Za-z0-9._-]+)+' "${scan[@]}" 2>/dev/null \
    | grep -v '^node_modules/' | sed -E 's/[.-]+$//' | LC_ALL=C sort -u || true)"
[[ -n "$refs" ]] || exit 0

# Prints yes | no | unknown for "./<subpath>" against a package.json's exports map. `unknown` covers
# every shape that carries no authoritative subpath list; only `no` is ever reported.
resolves() {
    jq -r --arg s "./$2" '
        def matches($key): if ($key | contains("*"))
            then ($key | split("*")) as $p
                | if ($p | length) == 2 then ($s | startswith($p[0])) and ($s | endswith($p[1])) else true end
            else $key == $s end;
        (.exports // null) as $e
        | if $e == null then "unknown"
          elif ($e | type) == "string" then "no"
          elif ($e | type) != "object" then "unknown"
          else ($e | keys) as $k
            | if ([$k[] | startswith(".")] | any) == false then "no"
              elif ([$k[] | select(matches(.))] | length) > 0 then "yes"
              else "no" end
          end' "$1" 2>/dev/null || printf 'unknown'
}

printf '%s\n' "$refs" | while IFS= read -r ref; do
    [[ -n "$ref" ]] || continue
    rest="${ref#@cratis/}"; pkg="${rest%%/*}"; sub="${rest#*/}"
    manifest="node_modules/@cratis/$pkg/package.json"
    if [[ ! -f "$manifest" ]]; then report "$ref — skipped (@cratis/$pkg not installed)"; continue; fi
    status="$(resolves "$manifest" "$sub")"
    version="$(jq -r '.version // "?"' "$manifest" 2>/dev/null || printf '?')"
    if [[ "$status" != "no" ]]; then report "$ref — $status (@cratis/$pkg $version)"; continue; fi

    # Bounded so `.../CommandForm` does not answer for `.../CommandForm/fields`. Qualification is
    # judged per (file, reference): the corpus states a subpath's version requirement once and may
    # then mention it again unqualified in the same file, so any qualified line clears the file.
    bounded="($(printf '%s' "$ref" | sed 's/\./\\./g'))([^A-Za-z0-9._/-]|\$)"
    { grep -rlE "$bounded" "${scan[@]}" 2>/dev/null || true; } | LC_ALL=C sort | while IFS= read -r file; do
        # `grep -n` prefixes each hit with `<line>:`, which carries no dot and so cannot itself look
        # like a version. Lines where the reference is part of a node_modules path are dropped again
        # here — the extraction pass already ignores them, and they are not module specifiers.
        hits="$(grep -nE "$bounded" "$file" 2>/dev/null | grep -vF "node_modules/$ref" || true)"
        [[ -n "$hits" ]] || continue
        # A here-string, never `printf … | grep -q`: an early-exiting `grep -q` closes the pipe,
        # `printf` dies of SIGPIPE with 141, and under `pipefail` the pipeline reports failure even
        # though the version matched — turning a correctly-qualified line into a warning.
        if grep -qE "$version_re" <<<"$hits"; then
            report "$ref — missing from @cratis/$pkg $version but version-qualified in $file"
            continue
        fi
        line="$(printf '%s\n' "$hits" | head -1 | cut -d: -f1)"
        warn "$file:$line: '$ref' is not in the exports map of the installed @cratis/$pkg $version — fix the reference, or mark the line with the version it needs (e.g. '(≥ 3.0.0)')"
    done
done

# Tier 2 over the same roots: a subpath that resolves says nothing about the *names* imported
# through it. Kept in its own script — a different question, a different corpus extraction and a
# different report variable — and invoked from here so the single call site in validate-ai-setup.sh
# gets both. Tested with -f, not -x, and run through `bash`: a checkout that lost the exec bit must
# not silently drop the guard.
imports="$(dirname "${BASH_SOURCE[0]}")/validate-package-imports.sh"
if [[ -f "$imports" ]]; then bash "$imports" "$@" || true; fi
