---
name: cratis-screenplay-model-authoring
description: Author, inspect, refactor and verify a Cratis Screenplay .play model through its typed AST and MCP workspace tools. Use for creating or changing model elements, navigating large split models, reviewing references/specifications, revision-checked screen/UI authoring proposals, or distinguishing source validity from executable/runtime readiness. Do not use for rendering a model into an application or hand-written Arc/Chronicle code.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/SKILL.md -->

# Author a Screenplay model through its AST

Read [preferred syntax](../cratis-screenplay-toolchain/references/preferred-syntax.md) when writing or reviewing `.play`.

Prefer the Screenplay MCP tools to text/regex edits: the compiler owns declaration,
reference, hierarchy and source evidence for the declarative document set.

**Valid source is not necessarily executable.** Stage owns rendering/runtime
admission. Read the [language reference](references/language-reference.md) for
construct syntax, source-versus-execution states and a complete compiled model.

For screens, read `references/screen-authoring-workflow.md`; use the
same model root or workspace export for all checks. Syntax: `cratis-screenplay-ui-composition`.

Record open modeling questions as `question` notes in committed
`.screenplay/notes.json`, not chat or `description` text. Never put secrets or
personal data in notes. Read [model notes](references/model-notes.md) when
listing, recording or resolving questions: `list-model-notes`,
`propose-model-notes`, `read-proposal view=notes`, refs and orphan review.
Application, Module, Feature and Slice notes anchor before binding; members
still need binding and otherwise substitute their own slice. Inspect
`containerIdentities` in `open-workspace` / `workspace-state` for missing or
ambiguous identities; fix parse/placement errors first. Read the notes reference
for refusals, `ambiguous: true`, typed targets and every `substitutions` entry.

## Choosing a strategy

Read [shop strategies and entropy](references/choosing-generation.md) before
editing concept generation or `generated identifier`.

## When not to

For generation proofs/PLAY0688 repairs, read [generation details](references/mcp-tools.md#concept-generation):
use `rules[]`, not parsed diagnostics (Requires Screenplay 4.173.0,
`screenplay --version`; older servers omit proofs).

## Locate and connect

Use the declared `screenplay` MCP server (for example `open-workspace`).

Never ask the user which folder to use before trying `open-workspace` without
arguments; do not invent a root. Read [MCP loop](references/mcp-loop.md) when connecting or switching roots;
it owns fallback discovery, fixed-root limits and the initialization protocol.
Discover `tools/list` schemas.

Look first for the project's existing `.play` files: their folder is the model.
New models go in `Source/` or `src/`, else root `Screenplay/`, never `.cratis/`
(configuration/shared corpus only). The source is the single flow model;
explanatory Markdown belongs in repository documentation.

`cratis ai install` manages `.cratis/ai/`, never model files. Select the
`cratis/screenplay` profile in the project's AI configuration. The corpus-owned
`mcp-servers.json` declares the Screenplay server; supported client registration
is owned by the Cratis CLI, which preserves other servers and user configuration.

The CLI-bundled MCP can be older than the standalone tool: discover schemas.
Read [MCP loop](references/mcp-loop.md) for supported launch channels; do not
substitute a global tool, Docker or copied managed configuration.

If MCP is unavailable, report it; regex edits are not compiler-validated AST
editing. Existing standalone `screenplay mcp` users may retain that entry point.

## Understand before editing

1. Start with `describe-application` counts and paged logical children. A folder
   of `.play` files is one application, not independent files.
2. Use `search-declarations`, `declaration-details`, `find-references` and
   `dependencies` to locate the owning declaration and affected references.
   Before a property change, read [property links](references/property-links.md):
   `kind: "Property"`, `Owner.member` / `Owner.generationN.member`, direct mappings.
   `complete: false` is incomplete evidence, never a successful empty list.
   Edit through MCP proposals, not hand-edited text; [generation](references/mcp-tools.md#concept-generation).
3. Read diagnostics and coverage limitations. Identify subjects by code and typed
   `arguments`, never by parsing message text. Read [diagnostic arguments](../cratis-screenplay-toolchain/references/diagnostic-arguments.md)
   for kinds, PLAY0524 step paths/indices, unknown fields and older responses. Use `dependency-graph` for container
   edges, declared-dependency coverage, cycles and advisory order; the board's
   dependency-map toggle exposes this evidence visually. Unknown or ambiguous
   bindings are not an empty, successful dependency graph.
4. Use `find-fixtures` for value occurrences (including example/override origins
   and event routes) and `find-assertion-gaps` for authored assertion gaps. Neither
   executes specifications; use `run-specifications` for that. `diagnostics` accepts `scope` and opt-in `checks`;
   skipped completeness checks are not passes. Details: `references/mcp-tools.md`.
5. Echo `sourceRevision` on subsequent pages. Restart the query after drift;
   never combine pages from different snapshots.
6. For screens, also discover the template, form, toolbar, package/icon and
   outlet declarations that provide context. A component binding with no traced
   data source is not a shortcut; fix the model source.

Scope reads to the relevant container/document, not the whole merged AST.
Size-limit refusals require narrower reads, not truncation.

## Application modes and described handler intent

Start new models with `reaction batches atomic`; preserve it through proposals.
Read [language admission](../cratis-screenplay-toolchain/references/language-admission.md)
for atomic/protected modes and Stage refusals. Read [intent inventories](references/mcp-tools.md#dependencies-and-intent-inventories)
for handler views: `executableReady` means binding, not an implemented body.

## Compare models

Compare with `main` through `semantic-diff` worktree `path`, not two exports.
Read [comparison sources](references/mcp-tools.md#semantic-revision-comparison) for paging.

## Make one coherent proposal

**One edit strategy.** Prefer discovered identity-preserving operations
(`propose-rename`, `propose-repair`, `propose-extract-inline-event`, `propose-ast`).
Bounded text edits must leave catalog addresses unchanged (descriptions, rule
bodies, existing-member mappings), never a way around an MCP refusal. When `.screenplay/identities.json`
exists, adding, removing or renaming a declaration, command or read-model property, query,
query argument or specification goes through MCP, and an `id` pin is not enough for a rename or move. A request naming
a rename, move or removal is the approval: do not ask again at `apply`. Without MCP, text-rename
with `id "<Old>"` pins and an identity note; only with `identities.json` and no MCP, return the
request to the owning session, saying first it is NOT done.
Proposals are connection-local (16 cap); only the owner proposes/applies.
Others send an edit request ([MCP loop](references/mcp-loop.md)).

- Apply is not crash-atomic across files: it journals its inverse first. The model
  root must be trusted and exclusively writable during effects.

1. `open-workspace` obtains workspace/catalog revisions and restores root-local
   identity state. `read-workspace` locates document identities.
2. `read-ast` returns original occurrence handles and existing semantic IDs.
   Query `syntax-schema` for exact node members before constructing typed input.
3. Prefer `propose-rename` and `propose-move`: they carry identities and notes
   even on unbound models. Manual `propose-ast` / `propose-source` renames need
   `semanticRenames`; removal needs retirement and reports `orphanedNotes`.
   Never substitute global string replacement for an explicit refusal.
4. Prefer `propose-property` for property add/rename/remove, not hand-composed
   `propose-ast` batches. Inspect the chain and never guess past a refusal:
   [property links](references/property-links.md). Otherwise use `propose-source`
   (whole text) or `propose-ast` (typed edits); `propose` is executable-only.
   Default hand-written source to `PreserveExactSource` (Requires Screenplay 4.158.0,
   `screenplay --version`); read `references/mcp-loop.md` when choosing formatting.
5. Keep reference policy `Safe`. Use `Draft` only for deliberately requested
   unresolved model debt and report that debt; it does not waive structural,
   identity or binding-protection checks. Batch independent reference removals/additions;
   unmatched same-text pairs refuse as ambiguous. Preserve moves with `propose-move`
   or semantic migrations. String `conflicts[].document`/`path` locate refusals:
   [reference continuity and version gate](references/mcp-loop.md#reference-continuity-and-located-conflicts).
6. Report `summary`, inspect severity errors/codes rather than counts
   (Requires Screenplay 4.156.0, `screenplay --version`); read `references/mcp-loop.md`
   when reviewing PLAY0288, comment loss or introduced executable errors. Inspect `read-proposal` before/after bytes and diagnostics, including the
   identity-state change through `workspace-state`. Review `orphanedNotes` on
   source proposals; reanchor or resolve affected questions before applying
   ([notes and orphan impact](references/model-notes.md#source-proposals-can-orphan-notes)).
7. Run `run-specifications` with `proposalId` before applying; record V4 separately.
   PLAY0285 unbinds the whole model. Read the [MCP tool guide](references/mcp-tools.md#run-reference-specifications)
   for candidate paging and the required version.
8. `apply` only the reviewed server-produced proposal within the user's requested
   change. The proposal ID and before revisions are required; stale state rejects.
   Apply after checks if requested; otherwise show the proposal and ask once.
9. When the host draws views, `visualize-model` shows the board. It follows the files
   on disk while open, so after an apply do not re-request it; just say what changed.

Handles are revision-bound, not durable IDs. Modules/features span fragments;
after apply fetch fresh handles/revisions.

On refusal, use `failureKind`, never message text ([failures/limits](references/mcp-loop.md#failures-use-failurekind-never-message-text)).
After revision/disk/identity drift, reopen and re-propose. For `ApplyOutcomeUnknown`
or EOF during `apply`, **never retry apply**: reconnect, inspect `workspace-state`
and ask. Never delete `.screenplay/pending.json`. Limits reject, never truncate.

Choose formatting and disclose dropped comments before apply:
[formatting/limits](references/mcp-loop.md#formatting-and-limits) covers
`PreserveTrivia`, canonical consent and byte preservation.

`identityMigrationIssues`: read the [MCP loop](references/mcp-loop.md).

## Repair or extract before rewriting

Read `read-workspace` with `view: "repairs"` at the same revision as diagnostics and pass the repair to `propose-repair`; use `propose-extract-inline-event` before adding generations, or let a `remove` with `newGeneration: true` extract the inline event itself, as [property links](references/property-links.md) describes. `PLAY0469` changes the event contract, so ask about stored contracts first. No automatic generation-evolution repair ships. `PLAY0479`: migrate optionality spelling with `PreserveTrivia`. `eventNeverPersisted: true` cannot unfreeze admitted revisions. Differing event pins carry `storedName` in ESM v16, shared by all generations and unique (PLAY0472). Read [references/repairs.md](references/repairs.md) when fixing `PLAY0166`, `PLAY0478`, `PLAY0516` or `PLAY0683`, or admitting a [release baseline](references/release-baseline.md). Read [references/destination-intent-repair.md](references/destination-intent-repair.md) when `PLAY0470` needs a repair.

## Choose a readable layout

Use `recommend-layout` for options and review an `expand-layout` proposal; layout is not application semantics. Use one file per module when modules are simple. Use one file per slice when a large model needs isolated diffs. Each level above the slices is a barrel file. Read [references/layout.md](references/layout.md) when splitting files or writing import barrels.

## Preserve state and recover explicitly

Fix binding errors through MCP proposals, not outside MCP: reopening after an
external fix can require reconciliation of a persisted partial catalog.
Commit `.screenplay/identities.json` with the model; only `apply` writes it. Never delete or overwrite identity state, mint IDs on restart, or text-rename identity-affecting declarations. A pending journal blocks normal work: inspect `workspace-state` and use `recover-workspace` only for the identified interrupted operation. Only `apply` / `recover-workspace` write source/catalog. `readiness.authoringAccepted` says the source is valid Screenplay; `executableReady` is separate. Read [references/state-and-recovery.md](references/state-and-recovery.md) for root conflicts, journals and readiness.

## Code attachments

Screenplay never compiles/runs opaque code attachments or `file` constraints;
a screen's `file` realizes UI instead. Read [MCP code attachments](references/mcp-tools.md#code-attachments)
for paths, PLAY0430–PLAY0434 and implementation-requirements views; hashes/maps
are not behavioral evidence.

## Verify and report honestly

- Require zero source errors; investigate warnings, reference safety, identity
  continuity and exact intended changes.
- Report which of the four states you checked: **parsed**, **bound**,
  **reference-executed**, **target-executed**. When distinguishing source validity from execution, read the
  [language reference](references/language-reference.md#source-validity-is-not-execution);
  it defines them and maps them onto V1 to V5. `screenplay --warnaserror` checks
  syntax plus the consistency rules (`PLAY0282`-`PLAY0294`) but never binds, so a
  clean run is not a bound or executable result.
- Never drop or stub source contracts to bind; the bundled compiler and renderer can be older than the standalone tool. Read [references/language-reference.md](references/language-reference.md) for the ESM version at which each construct binds.

- Unsupported capabilities never justify dropping constructs or inventing stubs.
  Validate intended execution with the owning downstream runtime.
- Model rejection cases as specifications; assertion presence is not execution.
- Check installed `tools/list`/schemas rather than assuming a remembered tool count.

The ordinary CLI remains useful for whole-folder validation:

```shell
cratis screenplay validate <model-folder> --warnings-as-errors
```

Keep screen transcripts in `.ai-work/`; check the same source, never an app that
silently drops authored UI. Use the Screenplay-owned canonical
corpus fixture path instead of copying fixtures here.

Never invent syntax or capabilities.

## Route near misses

| Need | Skill |
| --- | --- |
| Discovering the domain and event-modeling method | `cratis-screenplay-event-modeling` |
| Commands, events, validation and concurrency | `cratis-screenplay-command-surface` |
| Projections and reducers | `cratis-screenplay-projections` |
| Read models, queries and screens | `cratis-screenplay-read-surface` |
| UI composition, bindings, navigation and themes | `cratis-screenplay-ui-composition` |
| Captures, reactions and triggers | `cratis-screenplay-captures-and-reactions` |
| Behavioral examples and assertions | `cratis-screenplay-specifications` |
| Rendering/running an admitted model | `cratis-stage-rendering-and-sandbox` |
| Lifecycle and model-first decision | `cratis-screenplay-modeling-lifecycle` |
| Compiler, CLI, MCP and diagnostics | `cratis-screenplay-toolchain` |
| Event-model diagrams rather than .play source | `cratis-event-model-diagram` |
