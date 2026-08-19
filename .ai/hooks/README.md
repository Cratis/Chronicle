# Hooks — enforcement, not persuasion

Everything else in `.ai/` is text an agent may or may not follow. The files here are the part
that runs. They convert the mechanically-checkable Cratis invariants into deterministic checks
that fire whether or not the model remembered the rule.

Three layers:

| Layer | Event | Script | Cost | Effect |
|---|---|---|---|---|
| Pattern pass | `PostToolUse` on a write | `scripts/cratis-pattern-scan.sh` | zero tokens until a match | appends a one-line reminder to context, never blocks |
| Hard block | `PreToolUse` on a write | `scripts/cratis-guard-writes.sh` | zero | exits **2** — the write does not happen |
| Quality gate | `Stop` | `scripts/cratis-quality-gate.sh` | one build/test run, only when relevant files changed | exits **2** — the turn does not end |

They are wired for Claude Code in [`.claude/settings.json`](../../.claude/settings.json).
The markdown files in this folder (`agent-stop.md`, `pre-commit.md`) remain *lifecycle guidance* —
they describe what a hook should do for tools that have no wiring yet.

> `.ai/` is the source of truth (see [`../rules/managing-ai-rules.md`](../rules/managing-ai-rules.md)).
> Hooks are the one surface with no folder adapter: Claude reads `.claude/settings.json`,
> Copilot would read `.github/hooks/*.json`. Only the Claude wiring exists today.

## What is enforced

Rule numbers refer to the numbered list in [`../rules/general.md`](../rules/general.md).

**Blocked outright** (`PreToolUse`, exit 2):

- Editing a file whose header marks it as Cratis-generated output — rule 15 `[contract]`
- Writing content that opens with such a header (hand-authoring a "generated" proxy)
- `Directory.Packages.props`, `global.json`, `NuGet.config`, `yarn.lock`, `package-lock.json`,
  `pnpm-lock.yaml`, `packages.lock.json` — the Source-of-Truth Discipline rule
- `.env`, `.env.*`, `*.env` — secrets

The generated-file check is anchored: the marker must be a comment opener at the start of one of
the first five lines. A rule file or a document that merely *mentions* the marker is not blocked.

**Flagged** (`PostToolUse`, exit 0 + context):

| Pattern id | Rule | Detects |
|---|---|---|
| `cratis-automap-call` | 10 `[contract]` | `.AutoMap()` in a file that never calls `.NoAutoMap()` |
| `cratis-ieventlog-in-handle` | 14 `[contract]` | `IEventLog` in a `Handle(` signature, wrapping across up to 5 lines |
| `cratis-nullable-event-property` | 6 `[contract]` | a nullable property inside a type declared with `[EventType]` |
| `cratis-route-on-readmodel` | 12 `[contract]` | `[Route(` inside a type declared with `[ReadModel]` |
| `cratis-controller-base` | 1 `[contract]` | `: ControllerBase` in a file that imports `Microsoft.AspNetCore.Mvc` |
| `cratis-primereact-dialog-import` | 16 `[convention]` | `from 'primereact/dialog'` |

The two `within_type_attribute` patterns are not line greps — the scanner tracks C# attribute
blocks and type scope (positional record, multi-line declaration, or braced body), so a nullable
property is only reported when it really sits inside an `[EventType]`.

**Gated** (`Stop`, exit 2): the app-pinned commands from the Quality Gates table in
`general.md` and the steps in [`agent-stop.md`](./agent-stop.md) — Debug build, specs, Release
build (with `-p:CratisProxiesOutputPath=` per `general.md`, so the proxy generator does not
re-run and touch already-correct generated files), frontend lint / compile / compile-specs /
test, and `validate-ai-setup.sh` for corpus changes.

## The corpus validator

`scripts/validate-ai-setup.sh` sits outside the three layers: it validates `.ai/` itself, and both
the `Stop` gate and the `ai-corpus` CI job run it. Structural, adapter and Codex checks are
**fatal**; the content drift guards **warn**.

### Package subpath existence — `scripts/validate-package-subpaths.sh` (warn)

Every other drift guard asserts that a string should *not* appear. This one is the other direction,
and the only guard that knows what a package is. It extracts each `@cratis/<pkg>/<subpath>` the
corpus names — fenced blocks, inline spans and table cells alike — from `.ai/rules`, `.ai/skills`,
`.ai/agents` and `.ai/prompts`, then resolves it against the `exports` map of the package installed
in `node_modules`. The exports map is exact and machine-readable, so a miss is a genuine miss.
`.ai/hooks` is deliberately *not* one of the default roots — this page names bogus subpaths as
examples, and a guard that reports its own documentation is a guard people switch off.

It exists because nothing in the repository could catch documenting
`@cratis/components/Notifications` (a subpath that first ships in **3.0.0**) while the pin is
**2.6.1**. A prose-pattern matcher has no notion of a package, a version, or an exports map; a
developer following the corpus got a module-resolution failure.

**Warn, never fail — the tradeoff.** The observation is exact but the conclusion is not: "the corpus
names an API that does not exist" and "this repository is pinned behind the version the corpus
documents" look identical from the exports map. This script propagates to every Cratis repository,
and the `ai-corpus` CI job checks out the tree and installs nothing — so failing would be a
permanent no-op in CI while turning repos red locally for their own dependency pin. The warning
names the file, the line and the installed version, and leaves the judgement to a human.

**Silent when it cannot judge.** No `jq`, no `node_modules`, a package this repository does not
depend on, or a package published without an `exports` map: skipped without a word. "Not installed"
is not a finding.

**Version-qualified lines are not drift.** The corpus deliberately documents some 3.0.0+ APIs
against a 2.x pin, marked inline as `(**≥ 3.0.0**)`. A reference is cleared when a line mentioning
it in the same file also carries a version — a dotted number, an `N.x`, or either inequality
spelling. Qualification is judged per *(file, reference)* rather than per line, because the corpus
states a requirement once and then mentions the subpath again unqualified nearby; per-line matching
would fire on exactly the lines someone had just fixed correctly. The check is deliberately generous
in the same direction: it would rather miss a stale line than warn about a correct one.

**What it deliberately does not check.** Named imports (`import { Toaster } from '…'`) are Tier 2's
job, below; .NET types named in prose or in a C# type position are Tier 3's. This tier checks module
specifiers, nothing else.

Run it standalone, optionally over other roots, and add `CRATIS_HOOKS_SUBPATH_REPORT=1` to see every
reference and how it resolved rather than only the failures. It invokes Tier 3 before its own gates
and Tier 2 after its own work, over the same roots, so the single call site in
`validate-ai-setup.sh` gets all three.

### Named import existence — `scripts/validate-package-imports.sh` (warn)

Tier 2, and the reason it exists is that Tier 1's answer is not the whole question: a subpath that
resolves says nothing about the *names* imported through it. For every
`import { A, B } from '@cratis/<pkg>/<subpath>'` in the corpus — single-line, brace-on-its-own-line,
`import type`, `A as B` (the *imported* name is what has to exist), trailing `//` comments — it
checks each identifier against the `.d.ts` closure of the installed package and warns about the ones
that are not there. `Toaster`, `toastCommandResult`, `PasswordField`, `RatingField` and the rest are
real APIs of `@cratis/components` **3.0.0** and absent from **2.6.1**; Tier 1 caught the three
*subpaths* that moved with them, and the twelve *names* were found only by a human reading package
internals.

**Deliberately permissive, and here is the price.** A name passes when it appears as a *word
anywhere* in the package's `.d.ts` closure — not only in an export position, not only behind the
subpath it was imported from — and the closure follows `export … from '<other-package>'` re-exports
one level out to another installed package. Intra-package barrels (`export * from './X'`) need no
following, because the whole tree is read either way. That admits names the package merely
*references* (an imported PrimeReact symbol, a name in a doc comment) and it will not notice a name
imported from the wrong subpath of the right package. The trade is deliberate: a false warning
trains people to ignore the guard, a missed one costs a stale line. Measured over the corpus's 85
import statements / 134 bindings / 38 distinct *(package, name)* pairs plus a 36-pair all-valid
probe: **zero false positives**, and it still flags all twelve of the 3.0.0 names above when they are
written unqualified.

**Same warn-only, same silence, same version rule as Tier 1.** No `jq`, no `node_modules`, a package
this repository does not depend on, or a package that ships no `.d.ts`: skipped without a word. A
name is cleared when any line in the same file that mentions it also carries a version — judged per
*(file, name)*, for the same reason Tier 1 judges per *(file, reference)*.

**What it deliberately does not check.** Identifiers that never appear inside an `import { … }`:
prose mentions, JSX usages, and C# type positions are all invisible. It reads TypeScript import
statements, nothing else.

Run it standalone over any roots, and add `CRATIS_HOOKS_IMPORT_REPORT=1` to see every binding and how
it resolved rather than only the failures.

### .NET type existence — `scripts/validate-type-references.sh` (warn)

Tier 3, and the only tier that reads .NET rather than TypeScript. Tiers 1 and 2 both start from an
`import` statement, so a type the corpus names *only* in prose and in C# type positions is invisible
to both. That is exactly how `ReactorSideEffect` survived: never a module specifier, never an import,
told readers to return it from a reactor, shown with object-initializer syntax — and never a type in
any Chronicle release. Someone following the corpus wrote code that does not compile.

**The index.** Every `Cratis*` version pinned in `Directory.Packages.props`, plus the Cratis packages
those pull in (`Cratis` is a metapackage), resolved against the local NuGet cache. Each package's
`lib/**/*.xml` carries `<member name="T:Full.Namespace.TypeName">` — a complete machine-readable type
list — and every other identifier the docs mention is kept as a second, permissive accept list, in
the same spirit as Tier 2's "a word anywhere in the `.d.ts` closure". Names the corpus itself
declares, and names declared in this repository's own `Source/**/*.cs`, are accepted too: a worked
example that writes `public record AuthorRegistered(…)` before using it is not documenting a
framework API. A curated allowlist covers the rest — see below.

**Why it is narrow, and what that cost.** The naive version of this check is the reason the whole
tier nearly did not ship. Of the **1279** distinct PascalCase names it reads across 151 corpus files,
**599 — 47% — resolve nowhere**, because the corpus legitimately invents domain examples
(`AuthorRegistered`, `IAuthorService`), placeholders and prose nouns. A guard that cries wolf 599
times gets switched off, and then it protects nothing. So only two constructs are ever reported:

| Construct | Why it is safe | Measured |
|---|---|---|
| **Attribute position** — `[Name]`, `[Name<T>]`, `[Name(…)]` inside an inline code span or a fenced `csharp` block | attribute brackets are unambiguous C#, and a markdown link cannot live inside a code span, so the syntax alone identifies an API reference; `Name` and `NameAttribute` both count | 686 occurrences, 61 distinct names |
| **Framework-adjacent type token** — any other PascalCase token in a code span or a fenced `csharp` block that resolves nowhere **and** is a strict PascalCase-word-boundary *prefix* of a real Cratis type name | that is the fabrication signature: a half-remembered real family of names with a member coined that was never minted. `ReactorSideEffect` is a prefix of `ReactorSideEffectFailure`; `AuthorRegistered` is a prefix of nothing Cratis ships | takes the 599 unresolved down to **2** |

Both remaining names — `ICommand` and `IQuery`, which do not exist — are cleared by the absence rule
below, because the corpus's own point about them is exactly that. **Zero warnings on the real
corpus.**

**Constructs measured and rejected.** Each was extracted over the whole corpus and its unresolved
names counted before being dropped: `new TypeName` in a fenced `csharp` block (**17** false positives —
example events are constructed but never declared), `IInterfaceName` in a fenced `csharp` block (**17** —
invented example services like `IOrderRepository`), the same in an inline code span (**23** —
TypeScript interfaces and shouty prose such as `IMPORTANT`), and in bare prose (**2**, including the
plural `IDs`). None of them survives the "precision over recall" test on its own. They are all still
*read*; they simply have to earn a warning through framework-adjacency instead of through syntax.

**Three structural exclusions, no allowlist needed.** A token is skipped when it is preceded by `.`
(a member, not a type), when it is ALL-CAPS (`PII`, `IMPORTANT`), and when it is written as
`<Placeholder>` — the corpus's `<Module>/<Feature>/<Slice>` idiom, distinguished from a generic
argument list by the character before the `<`, which in C# is always an identifier character.

**Same warn-only and same version rule as Tiers 1 and 2, plus one of its own.** A name is cleared
when any line in the same file that mentions it carries a version, *or* says the thing does not
exist — `does not exist`, `no longer`, `never use`, `removed`, `deprecated`, `there is no` and
friends. Part of this corpus's job is naming APIs that are **not** real, and warning about a line
whose entire point is that the type is fictional would be the most annoying false positive of all.
The cost is stated plainly: reintroduce a fabrication into a sentence containing one of those
phrases and the guard stays quiet.

**Silent when it cannot judge.** No `Directory.Packages.props`, no local NuGet cache, or a cache
holding none of the pinned versions: skipped without a word. It needs no `jq` and no `node_modules`,
which is why Tier 1 invokes it *above* its own gates rather than beside the Tier 2 call — a backend-
only repository must still get this check. It adds about 1.4 s to `validate-ai-setup.sh`.

**The allowlist — `scripts/type-references-allowlist.txt`.** Thirteen entries, each with a written
justification: ASP.NET Core and BCL attributes that live in ref packs (which ship no XML docs at
all), Orleans and `Microsoft.Extensions.*` attributes from packages that ship none either, `[CliCommand]`
/ `[CliExample]` from the separate `Cratis/cli` repository, the Chronicle **Kernel**'s `WellKnown`,
and `@cratis/fundamentals`' TypeScript `JsonSerializer`. Every one was verified real before being
listed. An entry is a small lie the guard tells itself, so prefer widening the index whenever that
is possible, and never add a name you have not confirmed exists.

**What it deliberately does not check.** TypeScript — that is Tiers 1 and 2. Members, methods and
properties: `Provide()`, `.AutoMap()` and `EventStoreName.NotSet` are all invisible, and a fabricated
*member* on a real type would pass. And a fabricated type that is not a prefix of any real Cratis
name is invisible too — the adjacency filter is what buys the precision, and it is also the ceiling
on the recall.

Run it standalone over any roots, and add `CRATIS_HOOKS_TYPE_REPORT=1` to see every distinct name and
how it resolved rather than only the failures.

## Configuration is data, not code

Neither the pattern list nor the gate commands live in a script. A consuming repository
customises both without forking anything:

| File | Purpose |
|---|---|
| `scripts/cratis-patterns.json` | shipped pattern set; its header `$comment` documents every field |
| `scripts/cratis-patterns.local.json` | optional; merged over the above by `id` — add patterns, or set `"enabled": false` to silence one |
| `scripts/quality-gates.json` | shipped gates; `changed` globs decide when a gate runs, `requires` decides whether it *can* |

A gate whose `requires.commands` are not on `PATH`, or whose `requires.paths` do not exist, is a
**no-op with a message on stderr** rather than a failure — that is how a repository with no .NET
solution or no frontend stays quiet.

**Profile note.** The C# patterns are application-profile and scoped to `Source/**/*.cs` here. A
framework-profile repository (Arc, Chronicle, Fundamentals, Components — see
[`../rules/framework.md`](../rules/framework.md)) has no vertical slices and should disable them
in its `cratis-patterns.local.json`.

**One property gates the proxy generator.** The generator's MSBuild target is
`Condition="'$(CratisProxiesOutputPath)' != ''"`, so clearing that property with
`-p:CratisProxiesOutputPath=` is the *only* way to make it no-op. There is no
`DisableProxyGenerator` property — MSBuild silently accepts unknown `-p:` names, so passing one
looks like it works and changes nothing. `.github/workflows/planner-build.yml` matches the shipped
gates: Release clears the path, Debug does not, because `general.md` makes the Debug build the
canonical trigger for regenerating the TypeScript proxies the frontend phase depends on.

## Escape hatches

Each is an explicit, auditable opt-out — none of them is a default.

| Variable | Effect |
|---|---|
| `CRATIS_HOOKS_ALLOW_PROTECTED_WRITES=1` | allows one protected write; this is the "unless explicitly asked" case for dependency manifests |
| `CRATIS_HOOKS_SKIP_SCAN=1` | disables the pattern pass |
| `CRATIS_HOOKS_SKIP_GATE=1` | disables the quality gate |
| `CRATIS_HOOKS_GATE_DRYRUN=1` | prints which gates would run, and why, then exits 0 |
| `CRATIS_HOOKS_PATTERNS=<path>` | replaces the pattern file |
| `CRATIS_HOOKS_GATES=<path>` | replaces the gate file |
| `CRATIS_HOOKS_SUBPATH_REPORT=1` | prints every `@cratis/*` subpath reference and how it resolved, not only the failures |
| `CRATIS_HOOKS_IMPORT_REPORT=1` | prints every `@cratis/*` named import binding and how it resolved, not only the failures |
| `CRATIS_HOOKS_TYPE_REPORT=1` | prints every .NET type/attribute name the corpus mentions and how it resolved, not only the failures |

## Design constraints

- **POSIX-safe bash**, `set -euo pipefail`, quoted expansions, no `eval`. Verified on bash 3.2
  (macOS system bash) — no `mapfile`, no associative arrays, no GNU-only flags, `LC_ALL=C` on
  every sort and compare.
- **Gate commands are an argv array**, executed directly. They never pass through a shell.
- **`jq` is the only dependency.** Every script
  degrades to a silent no-op when it is missing — a hook must never break a session.
- **Fail safe.** Malformed config, empty stdin, a missing file, a binary file, a file over 2 MB:
  all exit 0 silently.
- **No secrets, no file dumps.** Gate output is capped at `maxOutputLines`; the pattern pass
  prints a path, a line number and a fixed message — never file content.
- **No re-entry.** The `Stop` hook returns immediately when `stop_hook_active` is true, so a
  blocked turn cannot loop.
- **Each pattern fires once per file per session**, tracked under
  `${TMPDIR}/cratis-hooks/<session-id>/`, so a long edit loop cannot flood context.
- **The gate never edits code.** It builds, tests and lints. The one side effect is that a Debug
  build regenerates TypeScript proxies, which is the documented purpose of that build.

## Verifying a change

The scripts read hook JSON on stdin, so they are directly testable:

```bash
# Pattern pass — expect exit 0, and JSON on stdout only when something matched
jq -nc '{session_id:"t", cwd:"'"$PWD"'", tool_name:"Edit",
         tool_input:{file_path:"'"$PWD"'/Source/Planner/Work/Starting/Starting.cs"}}' \
  | .ai/hooks/scripts/cratis-pattern-scan.sh; echo "exit=$?"

# Hard block — expect exit 2
jq -nc '{session_id:"t", cwd:"'"$PWD"'", tool_name:"Edit",
         tool_input:{file_path:"'"$PWD"'/Directory.Packages.props", new_string:"x"}}' \
  | .ai/hooks/scripts/cratis-guard-writes.sh; echo "exit=$?"

# Quality gate — show the dispatch plan without running anything
jq -nc '{session_id:"t", cwd:"'"$PWD"'", stop_hook_active:false}' \
  | CRATIS_HOOKS_GATE_DRYRUN=1 .ai/hooks/scripts/cratis-quality-gate.sh
```

The subpath guard takes corpus roots as arguments, so it is testable in both directions without
touching the corpus — point it at a scratch folder holding a known-bad reference, then at the real
roots. A one-sided test passes vacuously; run both.

```bash
# Negative — expect a warning naming the file and line
mkdir -p /tmp/scratch-corpus
echo "import x from '@cratis/components/ThisDoesNotExist';" > /tmp/scratch-corpus/drift.md
.ai/hooks/scripts/validate-package-subpaths.sh .ai/rules /tmp/scratch-corpus

# Positive — expect silence, and the report to show every real reference resolving
CRATIS_HOOKS_SUBPATH_REPORT=1 .ai/hooks/scripts/validate-package-subpaths.sh
```

Tier 2 is testable the same way, and wants a third run the subpath guard does not: a probe of names
that all genuinely exist. A guard that warns on everything passes the negative test just as well as
a correct one, so prove it stays quiet when it should.

```bash
# Negative — a fabricated name behind a subpath that resolves
mkdir -p /tmp/scratch-corpus
echo "import { CommandDialog, ThisNameDoesNotExist } from '@cratis/components/CommandDialog';" \
  > /tmp/scratch-corpus/drift.md
.ai/hooks/scripts/validate-package-imports.sh /tmp/scratch-corpus

# Discrimination — every name real, expect silence
echo "import { DataPage, MenuItem } from '@cratis/components/DataPage';" \
  > /tmp/scratch-corpus/drift.md
.ai/hooks/scripts/validate-package-imports.sh /tmp/scratch-corpus

# Positive — the real corpus, with the report showing every binding resolving
CRATIS_HOOKS_IMPORT_REPORT=1 .ai/hooks/scripts/validate-package-imports.sh
```

Tier 3 wants the same three runs, and its negative case is the one that motivated it. Put
`ReactorSideEffect` back into a scratch corpus and the guard must name it; a design that misses its
own motivating case is the wrong design.

```bash
# Negative — the confirmed fabrication, in prose and in object-initializer syntax
mkdir -p /tmp/scratch-corpus
printf 'A reactor may return a `ReactorSideEffect` to control where the event is appended.\n' \
  > /tmp/scratch-corpus/drift.md
.ai/hooks/scripts/validate-type-references.sh /tmp/scratch-corpus

# Discrimination — every name real, expect silence
printf 'Return `EventForEventSourceId`, or a `ReactorSideEffectFailure` from an `IReactor`.\n' \
  > /tmp/scratch-corpus/drift.md
.ai/hooks/scripts/validate-type-references.sh /tmp/scratch-corpus

# Positive — the real corpus, expect silence, with the report showing how each name resolved
CRATIS_HOOKS_TYPE_REPORT=1 .ai/hooks/scripts/validate-type-references.sh
```

Run `bash -n` on every script and `jq .` on every JSON file before committing. The hook scripts are
kept at **zero** `shellcheck --external-sources --severity=style` findings by a blocking CI job — run
it before committing too.

## Note on `.claude/settings.local.json`

That file currently carries `allow` entries for `Bash(git push *)` and `Bash(gh pr *)`. Local
settings take precedence over project settings, so they may override the `ask` entries this
layer adds in `.claude/settings.json`. Remove them there if you want the confirmation prompt back.
