#!/usr/bin/env bash
# Tier 2 of the package drift guard. Where validate-package-subpaths.sh asks whether a module
# specifier resolves, this asks whether the *names* imported through it exist: for every
# `import { A, B } from '@cratis/<pkg>/<subpath>'` the AI corpus writes, it WARNS about each
# identifier that appears nowhere in the installed package's `.d.ts` tree.
#
# It exists because a subpath that resolves says nothing about what is behind it. `Toaster`,
# `toastCommandResult`, `PasswordField`, `RatingField` and friends are real APIs of
# @cratis/components 3.0.0 and absent from 2.6.1; Tier 1 caught the three *subpaths* that moved with
# them, and the names themselves were only ever found by a human reading package internals.
#
# WARN, never fail, and silent when it cannot judge — for the identical reasons spelled out at the
# top of validate-package-subpaths.sh. A miss is exact; the conclusion drawn from it is not.
#
# Deliberately permissive, because a false warning is worse than a missed one. A name counts as
# present when it appears as a *word anywhere* in the package's `.d.ts` closure — not only in an
# export position — and the closure follows `export ... from '<other-package>'` re-exports one level
# out. That admits a name that is merely referenced by the types (an imported PrimeReact symbol, a
# name in a doc comment) and it still flags every one of the twelve 3.0.0 names above.
#
# Portable: bash 3.2 + grep + sed + awk, with `jq` as the one accepted dependency (absent -> silent
# no-op, per the hook design constraints in ../README.md).
#
# Usage: validate-package-imports.sh [root ...]      # default roots: .ai/rules .ai/skills .ai/agents .ai/prompts
#        CRATIS_HOOKS_IMPORT_REPORT=1 ...            # also print every binding and its resolved status
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
cd "$root"

warn() { printf 'ai-corpus warn: %s\n' "$1" >&2; }
report() { [[ "${CRATIS_HOOKS_IMPORT_REPORT:-0}" == "1" ]] && printf 'ai-corpus import: %s\n' "$1" >&2 || true; }

command -v jq >/dev/null 2>&1 || exit 0
[[ -d node_modules/@cratis ]] || exit 0

# `.ai/hooks` is deliberately not a default root: this file and ../README.md name deliberately-bogus
# identifiers as examples, and a guard that reports its own documentation is a guard people switch off.
if [[ $# -gt 0 ]]; then roots=("$@"); else roots=(.ai/rules .ai/skills .ai/agents .ai/prompts); fi
scan=()
for d in "${roots[@]}"; do [[ -d "$d" ]] && scan+=("$d"); done
[[ "${#scan[@]}" -gt 0 ]] || exit 0

# Same clearing rule, and the same generosity, as Tier 1: a line that carries a version alongside the
# name has declared the skew on purpose (`(**≥ 3.0.0**)`), so it is not drift.
version_re='[0-9]+\.[0-9x]+|≥|>='

q="'"

# One awk pass over the corpus emits `file <TAB> line <TAB> package <TAB> specifier <TAB> name` for
# every named binding of an `@cratis/*` import — single-line and brace-on-its-own-line forms alike,
# `import type`, `A as B` (the *imported* name is what has to exist), and trailing `//` comments.
# Anything that does not lex as a plain identifier is dropped rather than guessed at. The quote
# character arrives as `-v q` so the program itself never has to contain one.
extract="$(cat <<'AWK'
FNR == 1 { n = 0; buf = "" }
{
    # A blank line, or a second `import`, ends an unterminated buffer rather than being glued onto
    # it. Without that, a stray `import {` in prose swallows the real statement below it and the
    # names get read off one block while the specifier is read off another — which invents an
    # identifier that resolves nowhere, i.e. exactly the false positive this guard must not produce.
    if (n > 0 && ($0 ~ /^[[:space:]]*$/ || $0 ~ /^[[:space:]]*import[[:space:]]/)) { n = 0; buf = "" }
    if (n == 0) {
        if ($0 !~ /^[[:space:]]*import[[:space:]]/) next
        if (index($0, "{") == 0) next
        start = FNR; buf = ""
    }
    line = $0
    sub(/\/\/.*$/, "", line)
    buf = buf " " line
    n++
    if (line ~ ("from[[:space:]]*[" q "\"]") || n > 40) { emit(); n = 0; buf = "" }
}
function emit(   spec, pkg, names, parts, count, i, name) {
    if (match(buf, "from[[:space:]]*[" q "\"][^" q "\"]+[" q "\"]") == 0) return
    spec = substr(buf, RSTART, RLENGTH)
    sub("^from[[:space:]]*[" q "\"]", "", spec)
    sub("[" q "\"]$", "", spec)
    if (spec !~ /^@cratis\//) return
    pkg = substr(spec, 9)
    sub(/\/.*$/, "", pkg)
    if (match(buf, /\{[^}]*\}/) == 0) return
    names = substr(buf, RSTART + 1, RLENGTH - 2)
    count = split(names, parts, ",")
    for (i = 1; i <= count; i++) {
        name = parts[i]
        sub(/^[[:space:]]+/, "", name); sub(/[[:space:]]+$/, "", name)
        sub(/^type[[:space:]]+/, "", name)
        sub(/[[:space:]]+as[[:space:]].*$/, "", name)
        sub(/[[:space:]]+$/, "", name)
        if (name !~ /^[A-Za-z_$][A-Za-z0-9_$]*$/) continue
        printf "%s\t%d\t%s\t%s\t%s\n", FILENAME, start, pkg, spec, name
    }
}
AWK
)"

files=()
while IFS= read -r f; do files+=("$f"); done < <(find "${scan[@]}" -type f 2>/dev/null | LC_ALL=C sort)
[[ "${#files[@]}" -gt 0 ]] || exit 0

bindings="$(awk -v q="$q" "$extract" "${files[@]}" 2>/dev/null || true)"
[[ -n "$bindings" ]] || exit 0

# Every `.d.ts` reachable from a package: its own tree, plus one level out through re-exports to
# another installed package (`export { Messenger } from '@cratis/arc/messaging'`). Intra-package
# barrels (`export * from './X'`) need no following — the whole tree is read either way.
closure_dirs() {
    local pkgdir="node_modules/@cratis/$1" dep
    printf '%s\n' "$pkgdir"
    grep -rhE "^[[:space:]]*export[[:space:]][^;]*[[:space:]]from[[:space:]]*[\"$q]" "$pkgdir" --include='*.d.ts' 2>/dev/null \
        | sed -E "s/.*[\"$q]([^\"$q]*)[\"$q].*/\1/" \
        | grep -v '^\.' \
        | sed -E 's#^(@[^/]+/[^/]+|[^@/][^/]*).*#\1#' \
        | LC_ALL=C sort -u \
        | while IFS= read -r dep; do
            [[ -n "$dep" && -d "node_modules/$dep" && "node_modules/$dep" != "$pkgdir" ]] && printf 'node_modules/%s\n' "$dep"
          done
}

# Sorted by package so each closure is read exactly once.
printf '%s\n' "$bindings" | cut -f3,5 | LC_ALL=C sort -u | while IFS="$(printf '\t')" read -r pkg name; do
    [[ -n "$pkg" && -n "$name" ]] || continue

    if [[ "${current:-}" != "$pkg" ]]; then
        current="$pkg"; tokens=""; version="?"
        if [[ -f "node_modules/@cratis/$pkg/package.json" ]]; then
            version="$(jq -r '.version // "?"' "node_modules/@cratis/$pkg/package.json" 2>/dev/null || printf '?')"
            dirs=()
            while IFS= read -r d; do [[ -n "$d" ]] && dirs+=("$d"); done < <(closure_dirs "$pkg")
            if [[ "${#dirs[@]}" -gt 0 ]]; then
                tokens="$(grep -rhoE '[A-Za-z_$][A-Za-z0-9_$]*' "${dirs[@]}" --include='*.d.ts' 2>/dev/null | LC_ALL=C sort -u || true)"
            fi
        fi
    fi

    # No package, or a package that ships no type declarations: nothing authoritative to compare
    # against, so it is not a finding.
    if [[ -z "$tokens" ]]; then report "$name — skipped (@cratis/$pkg has no installed type declarations)"; continue; fi
    # A here-string, never `printf … | grep -q`: under `pipefail` an early-exiting `grep -q` closes
    # the pipe, `printf` dies of SIGPIPE with 141, and the pipeline reports failure even though the
    # name matched. On a token list this size that is not a rare race — it is every time, and it
    # manufactures false positives, which is the one thing this guard must not do.
    if LC_ALL=C grep -qxF -- "$name" <<<"$tokens"; then report "$name — yes (@cratis/$pkg $version)"; continue; fi

    # Qualification is judged per (file, name) exactly as in Tier 1: the corpus states a version
    # requirement once in prose and then writes the import unqualified in a fenced block below it,
    # so any line in the file that mentions the name and carries a version clears the file.
    # One warning per file — the first binding — so a page that repeats an example is not a flood.
    printf '%s\n' "$bindings" | awk -F'\t' -v p="$pkg" -v n="$name" \
        '$3 == p && $5 == n { print $1 "\t" $2 "\t" $4 }' \
        | LC_ALL=C sort -t"$(printf '\t')" -k1,1 -k2,2n -u | awk -F'\t' '!seen[$1]++' \
        | while IFS="$(printf '\t')" read -r file line spec; do
            hits="$(grep -nwF -- "$name" "$file" 2>/dev/null || true)"
            if [[ -n "$hits" ]] && grep -qE "$version_re" <<<"$hits"; then
                report "$name — missing from @cratis/$pkg $version but version-qualified in $file"
                continue
            fi
            warn "$file:$line: '$name' imported from '$spec' is not declared anywhere in the installed @cratis/$pkg $version — fix the name, or mark the line with the version it needs (e.g. '(≥ 3.0.0)')"
          done
done
