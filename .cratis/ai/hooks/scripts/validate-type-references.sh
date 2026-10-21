#!/usr/bin/env bash
# cratis-ai-managed: hooks/scripts/validate-type-references.sh
# Tier 3 of the package drift guard, and the only tier that reads .NET rather than TypeScript.
# Tier 1 asks whether a module specifier resolves; Tier 2 asks whether the names imported through it
# exist. Both are blind to a type the corpus only ever names in prose or in a C# type position — and
# that is exactly how `ReactorSideEffect` survived: never a module specifier, never an import, told
# readers to return it from a reactor, and never existed in any Chronicle release.
#
# Reporting every unresolved name is the obvious design and it is wrong: 599 of the 1279 distinct
# names this script reads — 47% — resolve nowhere, because the corpus legitimately invents domain
# examples (`AuthorRegistered`, `IAuthorService`), placeholders and prose nouns. So only two
# constructs are ever reported, and between them they take those 599 down to two:
#
#   1. ATTRIBUTE POSITION — `[Name]` / `[Name<T>]` / `[Name(...)]` inside an inline code span or a
#      ```csharp block. Attribute brackets are unambiguous C#, and a code span cannot be a markdown
#      link, so the syntax alone identifies an API reference. `Name` and `NameAttribute` both count.
#
#   2. FRAMEWORK-ADJACENT TYPE TOKEN — any other PascalCase token in a code span or ```csharp block
#      that resolves nowhere AND is a strict PascalCase-word-boundary *prefix* of a real Cratis type
#      name. That is the fabrication signature: someone half-remembers a real family of names and
#      coins a member of it that was never minted. `ReactorSideEffect` is a prefix of
#      `ReactorSideEffectFailure`; `AuthorRegistered` is a prefix of nothing Cratis ships.
#
# Constructs measured and rejected — `new TypeName`, and `IInterfaceName` fenced, spanned or in bare
# prose — are written up in ../README.md. They are still *read* here; they just have to earn a
# warning through rule 2 rather than on their syntax alone.
#
# WARN, never fail, and — when it genuinely cannot judge — LOUD, never silent. A could-not-run
# condition exits 2 with a reason on stderr: this script spent its first lifetime exiting 0 at the
# `Directory.Packages.props` gate in the corpus repository, so it never checked anything anywhere
# (issue #287), and the fabricated APIs it exists to catch survived precisely because of it.
# Needs no `jq` and no node_modules; it needs a local NuGet cache, and no cache is a could-not-run.
#
# Portable: bash 3.2 + grep + sed + awk + find. No jq, no network, nothing written outside a tempdir.
#
# Usage: validate-type-references.sh [--self-test] [root ...]   # default roots: .cratis/ai/rules .cratis/ai/skills .cratis/ai/agents .cratis/ai/prompts
#        --self-test                                            # seed the known fabrication into a scratch corpus and
#                                                                 # fail unless the guard names it (guards-and-fuses.md)
#        CRATIS_HOOKS_TYPE_REPORT=1 ...                         # also print every distinct name and its status
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
cd "$root"

warn() { printf 'ai-corpus warn: %s\n' "$1" >&2; }
cannot() { printf 'ai-corpus type-references: could not run: %s\n' "$1" >&2; exit 2; }
# `if`, not `A && B || C`: the trailing `|| true` was there to keep a disabled report
# from failing the caller, but it also swallowed a real printf failure, and shellcheck
# flags the shape (SC2015) for exactly that reason.
report() {
    if [[ "${CRATIS_HOOKS_TYPE_REPORT:-0}" == "1" ]]; then printf 'ai-corpus type: %s\n' "$1" >&2; fi
}

# Nothing authoritative to compare against is not a finding, but it is also not a pass. In a .NET
# solution the pins come from the central package management file; in this corpus repository they
# come from the tracked pin list of the exact product versions the skills verify against. Neither
# present, or a NuGet cache that holds none of the pinned versions, is a loud exit 2 — the reader
# must be able to tell "ran and found nothing" from "never looked" (exit-codes-and-wrappers.md).
self_test=0
if [[ "${1:-}" == "--self-test" ]]; then self_test=1; shift; fi
pins_file="$(dirname "${BASH_SOURCE[0]}")/cratis-nuget-pins.txt"
if [[ -f Directory.Packages.props ]]; then
    queue="$(awk 'match($0, /<PackageVersion[^>]*>/) {
            el = substr($0, RSTART, RLENGTH); id = ""; ver = ""
            if (match(el, /Include="[^"]*"/)) id  = substr(el, RSTART + 9, RLENGTH - 10)
            if (match(el, /Version="[^"]*"/)) ver = substr(el, RSTART + 9, RLENGTH - 10)
            if (id ~ /^Cratis/ && ver != "") print id " " ver
        }' Directory.Packages.props)"
elif [[ -f "$pins_file" ]]; then
    queue="$(sed -E 's/#.*//; s/[[:space:]]+/ /g; s/^ //; s/ $//' "$pins_file" | grep -v '^$' || true)"
    [[ -n "$queue" ]] || cannot "the corpus pin list $pins_file holds no package pins"
else
    cannot "neither Directory.Packages.props nor the corpus pin list $pins_file exists - nothing to verify against"
fi
nuget="${NUGET_PACKAGES:-$HOME/.nuget/packages}"
[[ -d "$nuget" ]] || cannot "no local NuGet cache at $nuget - restore the pinned Cratis packages before judging type references"

# `.cratis/ai/hooks` is deliberately not a default root: this file and ../README.md name a deliberately
# fabricated type as the worked example, and a guard that reports its own documentation is a guard
# people switch off.
if [[ $# -gt 0 ]]; then roots=("$@"); else roots=(.cratis/ai/rules .cratis/ai/skills .cratis/ai/agents .cratis/ai/prompts); fi
scan=()
for d in "${roots[@]}"; do [[ -d "$d" ]] && scan+=("$d"); done
[[ "${#scan[@]}" -gt 0 ]] || exit 0

tmp="$(mktemp -d 2>/dev/null)" || cannot "cannot create a temporary directory"
trap 'rm -rf "$tmp"' EXIT

# --self-test (guards-and-fuses.md): seed the motivating fabrication into a scratch corpus and
# require this guard to name it, while the real names beside it stay silent. A guard that can pass
# vacuously is worse than no guard, and this one did exactly that for its whole first lifetime.
if [[ "$self_test" == 1 ]]; then
    scratch="$tmp/scratch-corpus"
    mkdir -p "$scratch"
    # shellcheck disable=SC2016
    printf 'A reactor may return a `ReactorSideEffect` to control where the event is appended.\n' \
        > "$scratch/fabrication.md"
    # shellcheck disable=SC2016
    printf 'Return `EventForEventSourceId`, or a `ReactorSideEffectFailure` from an `IReactor`.\n' \
        > "$scratch/discrimination.md"
    # shellcheck disable=SC2016
    printf '```csharp\n[ReactorSideEffect]\npublic SomeHandler ...\n```\n' \
        > "$scratch/attribute.md"
    scan=("$scratch")
fi

# Same clearing rule, and the same generosity, as Tiers 1 and 2: a line carrying a version alongside
# the name has declared the skew on purpose. The second rule is this tier's own — the corpus's job
# includes naming things that do NOT exist (`the marker interfaces ICommand, IQuery ... do not
# exist`), and warning about a line whose entire point is that the type is fictional would be the
# most annoying false positive of all.
version_re='[0-9]+\.[0-9x]+|≥|>='
absence_re='(do|does|did) not exist|no longer|never (use|write|call|return|inject|reach)|removed|deprecated|obsolete|there (is|are) no|non-existent|not a real|fabricat'

# ---------------------------------------------------------------------------------------------
# 1. Which packages to believe. Every `Cratis*` version pinned by the central package management
#    file or by the corpus pin list above, plus the Cratis packages those pull in (`Cratis` is a
#    metapackage: Arc, Arc.Chronicle, Chronicle, …), resolved against the local NuGet cache.
#    Deliberately a *union* across whatever versions the closure names rather than NuGet's
#    single-version resolution — over-accepting costs a missed stale line, under-accepting costs a
#    false warning, and only one of those is unacceptable here.
# ---------------------------------------------------------------------------------------------
seen=""; libdirs=""; hops=0
while [[ -n "$queue" && "$hops" -lt 8 ]]; do
    hops=$((hops + 1)); next=""
    while IFS=' ' read -r id ver; do
        [[ -n "$id" && -n "$ver" ]] || continue
        case " $seen " in *" $id/$ver "*) continue ;; esac
        seen="$seen $id/$ver"
        pkgdir="$nuget/$(printf '%s' "$id" | tr '[:upper:]' '[:lower:]')/$ver"
        [[ -d "$pkgdir/lib" ]] || continue
        libdirs="$libdirs$pkgdir/lib
"
        next="$next$({ grep -ohE '<dependency id="Cratis[^"]*" version="[^"]*"' "$pkgdir"/*.nuspec 2>/dev/null || true; } \
            | sed -E 's/.*id="([^"]*)" version="([^"]*)".*/\1 \2/' | LC_ALL=C sort -u)
"
    done <<<"$queue"
    queue="$(printf '%s' "$next" | grep -v '^[[:space:]]*$' || true)"
done
printf '%s' "$libdirs" | LC_ALL=C sort -u | while IFS= read -r d; do
    [[ -n "$d" ]] && find "$d" -type f -name '*.xml' 2>/dev/null
done | LC_ALL=C sort -u > "$tmp/xml.txt"
[[ -s "$tmp/xml.txt" ]] || cannot "the local NuGet cache holds none of the pinned Cratis packages - no XML documentation to verify against"

# ---------------------------------------------------------------------------------------------
# 2. The index. `<member name="T:Full.Namespace.TypeName">` is a complete, machine-readable list of
#    the documented public types; the word list beside it is every identifier the docs mention at
#    all — `cref`s into the BCL, parameter names, `<see>` targets — and exists purely to accept, in
#    the same deliberately-permissive spirit as Tier 2's "a word anywhere in the .d.ts closure".
#    Names the corpus or this repository's own C# declares are accepted too: a worked example that
#    defines `AuthorRegistered` before using it is not documenting a framework API.
# ---------------------------------------------------------------------------------------------
{ tr '\n' '\0' < "$tmp/xml.txt" | xargs -0 grep -ho 'name="T:[^"]*"' 2>/dev/null \
    | sed -E 's/name="T:([^"]*)"/\1/; s/`[0-9]+$//; s/.*[.+]//' | LC_ALL=C sort -u > "$tmp/types.txt"; } || true
[[ -s "$tmp/types.txt" ]] || cannot "the pinned Cratis packages expose no documented public types - nothing to verify against"
{ tr '\n' '\0' < "$tmp/xml.txt" | xargs -0 grep -hoE '[A-Za-z_][A-Za-z0-9_]*' 2>/dev/null \
    | LC_ALL=C sort -u > "$tmp/words.txt"; } || true

find "${scan[@]}" -type f -name '*.md' 2>/dev/null | LC_ALL=C sort > "$tmp/files.txt"
[[ -s "$tmp/files.txt" ]] || cannot "no markdown files under the scan roots - nothing to scan"

decl_re='(^|[^A-Za-z0-9_])(record|class|interface|struct|enum|delegate)[[:space:]]+(struct[[:space:]]+)?[A-Z][A-Za-z0-9_]*'
{
    { tr '\n' '\0' < "$tmp/files.txt" | xargs -0 grep -hoE "$decl_re" 2>/dev/null || true; }
    { find Source -type f -name '*.cs' -print0 2>/dev/null | xargs -0 grep -hoE "$decl_re" 2>/dev/null || true; }
} | sed -E 's/^[^A-Za-z]*//; s/^[a-z]+[[:space:]]+([a-z]+[[:space:]]+)?//' | LC_ALL=C sort -u > "$tmp/declared.txt"

allowlist="$(dirname "${BASH_SOURCE[0]}")/type-references-allowlist.txt"
: > "$tmp/allow.txt"
if [[ -f "$allowlist" ]]; then
    sed -E 's/#.*//; s/[[:space:]]//g' "$allowlist" | grep -v '^$' > "$tmp/allow.txt" || true
fi

# ---------------------------------------------------------------------------------------------
# 3. The two constructs, extracted in one markdown-aware pass, and resolved against the index.
# ---------------------------------------------------------------------------------------------
# The program is written out rather than inlined, as in validate-package-imports.sh: it has to
# contain both quote characters and a bare backtick (markdown's code-span delimiter), and a quoted
# heredoc straight to a file keeps every one of them out of the shell's reach. Not a command
# substitution — bash 3.2 mis-lexes a backtick inside a here-document nested in `$( … )`.
cat > "$tmp/extract.awk" <<'AWK'
function ucount(s,   i, n, k) { k = 0; n = length(s); for (i = 1; i <= n; i++) if (substr(s, i, 1) ~ /[A-Z]/) k++; return k }
# Every PascalCase-word-boundary prefix of a real type, remembering one whole name as the anchor to
# quote back at the reader. `ReactorSideEffectFailure` contributes Reactor, ReactorSide and
# ReactorSideEffect; a token landing on one of those is a coined member of a real family.
function addprefixes(t,   i, n, p) {
    n = length(t)
    for (i = 2; i < n; i++) if (substr(t, i + 1, 1) ~ /[A-Z]/) { p = substr(t, 1, i); if (!(p in P)) P[p] = t }
}
function classify(name) {
    if (name in T || (name "Attribute") in T) return "type in the pinned Cratis packages"
    if (name in W || (name "Attribute") in W) return "named by the pinned Cratis XML documentation"
    if (name in D) return "declared by the corpus or by this repository"
    if (name in A) return "allowlisted"
    return ""
}
function emit(kind, name, line,   status, anchor) {
    status = classify(name)
    if (status == "") {
        anchor = ""
        if (kind == "attr") status = "UNRESOLVED"
        else if (ucount(name) >= 2 && (name in P)) { status = "UNRESOLVED"; anchor = P[name] }
        else status = "unresolved, not framework-adjacent"
        if (status == "UNRESOLVED") printf "FLAG\t%s\t%d\t%s\t%s\t%s\n", FILENAME, line, kind, name, anchor
    }
    if (!(name in reported)) { reported[name] = 1; printf "INFO\t%s\t%s\n", name, status }
}
function scanattrs(s, line,   pos, rest, ch, name) {
    while (match(s, /\[[A-Z][A-Za-z0-9_]*/)) {
        pos = RSTART; ch = (pos > 1) ? substr(s, pos - 1, 1) : " "
        name = substr(s, pos + 1, RLENGTH - 1); rest = substr(s, pos + RLENGTH)
        s = rest
        # An indexer (map[Key]), a bracket continuation, or a bracket inside a string literal
        # ("[NotSet]") is not an attribute; an attribute must also close as one, after optional
        # generics or arguments.
        if (ch ~ /[A-Za-z0-9_)\]"']/) continue
        if (rest !~ /^(<[^<>]*>)?(\([^()]*\))?\]/) continue
        if (length(name) >= 3) emit("attr", name, line)
    }
}
function scantypes(s, line,   i, n, start, run, prev, nxt, pp) {
    n = length(s); i = 1
    while (i <= n) {
        if (substr(s, i, 1) !~ /[A-Za-z0-9_]/) { i++; continue }
        start = i
        while (i <= n && substr(s, i, 1) ~ /[A-Za-z0-9_]/) i++
        run = substr(s, start, i - start)
        prev = (start > 1) ? substr(s, start - 1, 1) : ""
        nxt = (i <= n) ? substr(s, i, 1) : ""
        if (run !~ /^[A-Z]/) continue          # camelCase tail, or a lowercase identifier
        if (prev == ".") continue              # `EventStoreName.NotSet` — a member, not a type
        if (length(run) < 4) continue
        if (run !~ /[a-z]/) continue           # PII, IMPORTANT — acronyms and shouty prose
        # `<Slice>` / `<ObservableQuery>` is the corpus placeholder idiom. A generic argument list is
        # not: there, the `<` always follows an identifier character.
        if (prev == "<" && nxt == ">") { pp = (start > 2) ? substr(s, start - 2, 1) : ""; if (pp !~ /[A-Za-z0-9_]/) continue }
        emit("type", run, line)
    }
}
BEGIN {
    while ((getline l < types)    > 0) { T[l] = 1; addprefixes(l) }
    while ((getline l < words)    > 0) W[l] = 1
    while ((getline l < declared) > 0) D[l] = 1
    while ((getline l < allow)    > 0) A[l] = 1
    fence = 0
}
FNR == 1 { fence = 0; lang = "" }
{
    if ($0 ~ /^[[:space:]]*(```|~~~)/) {
        if (fence == 0) { fence = 1; lang = $0; sub(/^[[:space:]]*(```|~~~)[[:space:]]*/, "", lang); sub(/[[:space:]].*$/, "", lang) }
        else { fence = 0; lang = "" }
        next
    }
    if (fence == 1) {
        if (lang == "csharp" || lang == "cs") { scanattrs($0, FNR); scantypes($0, FNR) }
        next
    }
    # Outside a fence only inline code spans are read: a markdown link cannot live inside one, which
    # is what makes `[Name]` unambiguous, and prose PascalCase is ordinary English.
    n = split($0, seg, "`")
    for (i = 2; i <= n; i += 2) { scanattrs(seg[i], FNR); scantypes(seg[i], FNR) }
}
AWK

# LC_ALL=C throughout: the corpus is full of em dashes, arrows and emoji, and an awk built against a
# UTF-8 locale aborts the whole pass on the first byte it cannot convert. Every pattern above is
# ASCII, so reading bytes loses nothing — a multibyte character is simply not an identifier character.
{ tr '\n' '\0' < "$tmp/files.txt" | LC_ALL=C xargs -0 awk -f "$tmp/extract.awk" \
    -v types="$tmp/types.txt" -v words="$tmp/words.txt" -v declared="$tmp/declared.txt" \
    -v allow="$tmp/allow.txt" > "$tmp/out.txt"; } || true

if [[ "$self_test" == 1 ]]; then
    flags="$(grep '^FLAG' "$tmp/out.txt" 2>/dev/null || true)"
    printf '%s\n' "$flags" | grep -q $'\tReactorSideEffect\t' \
        || { printf 'ai-corpus type-references self-test: FAILED - the seeded ReactorSideEffect fabrication was not flagged\n' >&2; exit 1; }
    if printf '%s\n' "$flags" | grep -qE $'\t(EventForEventSourceId|IReactor|ReactorSideEffectFailure)\t'; then
        printf 'ai-corpus type-references self-test: FAILED - a real Cratis type was flagged as unresolved\n' >&2; exit 1
    fi
    printf 'ai-corpus type-references self-test: passed - the seeded fabrication is named and the real types stay silent\n' >&2
    exit 0
fi

while IFS="$(printf '\t')" read -r _ name status; do
    # `|| true` is load-bearing, not decoration: an empty field would leave a failing `&&` list as
    # the last command in the loop body, and `set -e` would end the whole guard right there.
    { [[ -n "$name" ]] && report "$name — $status"; } || true
done < <(grep '^INFO' "$tmp/out.txt" 2>/dev/null || true)

# One warning per (file, name) — the first occurrence — so a page that repeats an example is not a
# flood. Qualification is judged per (file, name) exactly as in Tiers 1 and 2.
{ grep '^FLAG' "$tmp/out.txt" 2>/dev/null || true; } \
    | LC_ALL=C sort -t"$(printf '\t')" -k2,2 -k5,5 -k3,3n -u | awk -F'\t' '!seen[$2 "\t" $5]++' \
    | while IFS="$(printf '\t')" read -r _ file line kind name anchor; do
        # A here-string, never `printf … | grep -q`: under `pipefail` an early-exiting `grep -q`
        # closes the pipe, `printf` dies of SIGPIPE with 141, and the pipeline reports failure even
        # though the line matched — manufacturing exactly the false positive this guard must not make.
        hits="$(grep -nwF -- "$name" "$file" 2>/dev/null || true)"
        if [[ -n "$hits" ]] && grep -qE "$version_re" <<<"$hits"; then
            report "$name — unresolved but version-qualified in $file"; continue
        fi
        if [[ -n "$hits" ]] && grep -qiE "$absence_re" <<<"$hits"; then
            report "$name — unresolved but named as absent on purpose in $file"; continue
        fi
        if [[ "$kind" == "attr" ]]; then
            warn "$file:$line: '[$name]' is not an attribute in the pinned Cratis packages — fix the name, add it to .cratis/ai/hooks/scripts/type-references-allowlist.txt if it belongs to another framework, or mark the line with the version it needs (e.g. '(≥ 17.0.0)')"
        else
            warn "$file:$line: '$name' is not a type in the pinned Cratis packages, but '$anchor' is — fix the name, or mark the line with the version it needs (e.g. '(≥ 17.0.0)')"
        fi
      done
