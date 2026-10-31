<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/mcp-loop.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# The Screenplay MCP loop

## Contents

- Connect
- Who owns the connection
- The loop
- One edit strategy
- Reference continuity and located conflicts
- Self-describing identity migration refusals
- Identity state (`.screenplay/identities.json`)
- Read-only views worth knowing
- Failures (use `failureKind`, never message text)
- Formatting and limits
- Provenance

The working loop for the session that owns the model connection: connect, orient,
choose an edit route, propose, review, run specifications, apply, verify. Tool-by-tool arguments and
refusals are in the [MCP tool guide](mcp-tools.md); verdict names (V1 to V5) are
defined by `cratis-screenplay-modeling-lifecycle`. Discover the installed tool set and views with
`tools/list`: the CLI-bundled server can differ from the standalone one.

## Connect

Never ask which folder to use before trying `open-workspace` with no arguments.
Without a fixed root, it binds the client's single workspace root, else a launch
directory holding `.play` files. Only after a no-root or multiple-roots response,
pass `path`: the folder holding the project's models, otherwise the folder the
user names. Without a project the server uses `Documents/Screenplay` in the user's
home; do not invent a location. A fixed root can switch only to the same model
folder in a registered Git worktree of the same repository.

1. Launch with a **fixed root**: `screenplay mcp <model-folder>`, or
   `cratis screenplay mcp <model-folder>`, or `cratis screenplay mcp` inside a
   project, where the CLI locates the model (the project's `.play` files, else `Source/`
   or `src/`, else a new `Screenplay/` folder; what the `cratis/screenplay` profile registers). Stdio, newline-delimited
   JSON-RPC, one sequential connection.
2. Send `initialize`, then the `notifications/initialized` notification, **before
   any tool call**. Earlier calls fail with `-32600` "Initialize and send
   notifications/initialized before using tools".
3. Pass a physical path. Symbolic links and reparse points are refused, including
   `.screenplay` metadata.
4. Read `tools/list` schemas once per connection; never hard-code counts or
   argument lists. MCP-Apps hosts also expose `visualize-model`.
5. (Requires Screenplay 4.81.0 or later.) A startup root may switch to the same relative model folder in a
   registered Git worktree of its repository. `open-workspace.path` accepts the
   checkout or exact model folder. Shared Git directory and registration
   back-pointer must agree physically; unrelated/malformed/unregistered roots,
   submodules and `--separate-git-dir` checkouts refuse (`RootChangeRefused`).
   Switching clears proposals and cached state, then uses that worktree's own
   identities/journal. A pending journal blocks only its root. If an opened
   worktree is removed, explicitly return to the startup root; if the startup
   root is removed, start a new connection. No simultaneous named workspaces.

### Dynamic roots and the workaround

A server started **without** a root binds one on first use: `open-workspace.path`, then the
client's `roots/list` root, then a working-directory or fallback model folder. Avoid depending on
that by fixing the root at launch (the forms in step 1). The `cratis/screenplay` profile
registration fixes it in an installed project, and the CLI locates the model itself, so no model
directory has to exist.

Requires Screenplay 4.63.2 or later (standalone, or a Cratis CLI 3.28.2 or later that bundles it)
for a server without a root facing a client that advertises roots. Before it, the server read the
roots reply from the wrong member and answered with an error with `id: null` immediately after
`notifications/initialized`, before any tool call; real hosts treat that as fatal and drop the
connection. Passing `open-workspace.path` does **not** avoid it, because the error is sent before
`open-workspace` runs. On such a tool, fix the root at launch.

## Who owns the connection

- **Proposals are connection-local.** They live in memory, at most **16**
  outstanding (`LimitExceeded`); `open-workspace`, a successful `apply` and a root
  change clear them. A proposal id means nothing in another process, another
  session or after a reopen.
- **One owner** (normally the main session) holds the connection and does
  open, propose, review, approval, `apply`. Only `apply` and `recover-workspace`
  write.
- **Non-owners** (reviewers, renderers, explainers, subagents without MCP) never
  return proposal ids. They return a bounded **edit request**; the owner
  re-proposes it on its own connection:

  ```text
  root: <model root>
  beforeRevision / catalogRevision: <the values read>
  target: <qualified address and kind>
  change: <text diff or typed intent>
  rationale: <why>
  expected impact: <diagnostics, identity effect, specifications>
  ```

- Read-only orientation (`describe-application`, `search-declarations`,
  `diagnostics`, and `read-workspace` views) is fine from any session that has its
  own short-lived connection.
- Discover capabilities once per session (tool, version, `tools/list` schemas,
  root, configuration) and reuse them. Refresh when the root, tool, version or
  configuration changes or a call fails. Never reuse a stale revision, catalog
  revision, handle or proposal.
- Identify a declaration by its qualified address (module, feature, slice, name)
  and load only that scope plus what it depends on. Do not keep a duplicate JSON
  copy of the model. Bind every edit request and proposal to the revision you read.

## The loop

1. `tools/list` once per connection.
2. Orient cheaply: `describe-application` summary, `diagnostics`, then scoped
   `search-declarations`, `declaration-details`, `dependencies`/`dependency-graph`.
   Diagnostics `scope` includes descendants and direct dependents; `checks` opts
   into completeness warnings (skipped on whole-source errors). Paging with
   `offset > 0` requires `expectedSourceRevision`. Seed coverage with
   `find-specification-obligations` (presence, not execution), use
   `find-modeling-smells` as questions needing a domain consequence, and compare
   generated/curated exported snapshots with `semantic-diff`.
   Before a property change, use `find-references`/`dependencies`
   `kind: "Property"` and `Owner.member` or historical event
   `Owner.generationN.member`. Follow direct mapping counterparts one query at a
   time; inspect incomplete/ambiguous evidence and `reasons`. `complete: false`
   is not an empty successful result. Shapes/roles: [property links](property-links.md).
   Inspect concept `generate` with `declaration-details`
   `kind: "Concept"`, `view: "summary"`: `generation.executable.status` distinguishes
   `admitted`, `refused`, `notAssessed`; empty local error codes do not prove binding.
   A bundle that predates this summary also lacks the entropy repairs.
   Fields and tagged evidence: [concept generation](mcp-tools.md#concept-generation).
3. `open-workspace` and keep `revision` and `catalogRevision`. Opening writes
   nothing. With `identityPersistence: "root-local-on-apply"` the identities file
   is written only by `apply`.
4. Choose the edit route (next section). For typed edits: `syntax-schema` for the
   node kind, then `read-ast` with `includeContent` for revision-bound handles.
   Never invent a handle.
5. Prefer `propose-property` for supported property add/rename/remove chains,
   not hand-composed `propose-ast` batches; inspect `incomplete`, `needsDecision`
   and `chainCandidates` before choosing sources, fixtures or event evolution.
   Exact arguments and decisions: [property links](property-links.md).
   Otherwise propose one coherent change (a slice works best): `propose-source` for whole
   `.play` text, `propose-ast` for focused typed edits, with
   `validation: "Authoring"`, `referencePolicy: "Safe"` (`Draft` only for
   deliberate, reported gaps). For hand-written source use `propose-source` with
   `formatting: "PreserveExactSource"` to keep supplied text, even structural edits.
   For typed AST value/mapping patches use `PreserveTrivia`; structural AST edits
   need `CanonicalizeTouchedDocuments` (reprints the touched files).
6. Report the proposal from its one-paragraph `summary` (also the second text
   block after JSON), rather than inventing a summary from counts. Review
   `authoringDiagnosticSeverities.errors` and `codes`: nonzero
   `authoringDiagnosticCount` counts proposal checks, including PLAY0288 canonical
   printing even with no dropped comments; `after.readiness` counts resulting-source
   diagnostics only. Page `read-proposal view=diagnostics`, then changes,
   before/after per document and `dropped-comments`; keep checking
   `introducedExecutableErrors` and `droppedCommentCount`. On
   MCP-Apps hosts `visualize-model` with the `proposalId` shows Current and
   Proposed, and its `sketch` argument previews a what-if of whole documents
   without writing. An
   unintended executable regression is discarded (`discard-proposal`) and
   re-proposed. Intent the user has accepted that does not run yet (operations,
   the `numbers exact` source mode; PLAY0268 by design)
   may be applied from a reviewed `Authoring` proposal: disclose the new
   diagnostics, report V3 as not binding-ready, and never drop or stub the
   construct. The server's own guidance is to apply such a proposal only when
   the user accepts a model that does not run yet.
7. Run `run-specifications` with the
   retained `proposalId` before `apply`. This read-only V4 route runs the candidate,
   including retained identities and planned renames, without applying, refreshing
   from disk or discarding it. `read-proposal view: "semantic-diff"` is static
   comparison, not execution. Record counts and outcomes; an unbound candidate
   returns executable diagnostics and executes nothing. An impossible `then`
   (PLAY0285) unbinds the whole model, not just that specification.
   For continuation pages, reuse `proposalId` and `expectedSourceRevision` from
   the first response (required for `offset > 0`). Applied, discarded or reopened
   proposals give `UnknownProposal`. A changed disk base can still run the retained
   candidate; it does not make a stale `apply` safe. Without `proposalId`, the
   tool runs the current workspace, not the candidate.
8. Get approval unless the user already approved this effect and target (ask again
   only when target or consequence expands). A request or brief that names a rename,
   move or removal is that approval, also unattended: do not ask again per apply. Then `apply` with the **before**
   revisions. Every old handle and proposal is gone afterwards: re-read at the new
   revision.
9. Verify: compile the whole folder (V1), read `executable-diagnostics` (V2,
   V3), and run `screenplay test <root> --format json` or workspace
   `run-specifications` (V4), naming the tool, version and source revision. A
   candidate V4 applies only to that candidate; never relabel it as disk evidence.
   CLI exits: 0 passed, 1 failed, 2 invalid arguments/input/selection or IO,
   3 unsupported/unbound. `test` rejects `--no-color`. Hand off to the verdict
   vocabulary; do not restate a state you did not run.

## One edit strategy

1. **Discover** what the installed server offers (`tools/list`, `repair-capabilities`).
2. **Prefer typed, identity-preserving operations**: `propose-rename` for renames
   (keeps `id` pins and assigned identities, refuses on ambiguity),
   `propose-repair`, `propose-extract-inline-event`, `propose-ast` for additions,
   replacements, removals and moves. Use `propose-source` when authoring whole
   documents as UTF-8 text, not base64 or typed node JSON; `propose` remains the
   executable-only exact-byte interface. Both authoring routes require revisions,
   formatting consent and identity continuity. `SourceParseFailed` returns located
   `authoringDiagnostics` without a proposal. Details: `mcp-tools.md`.
3. **Bounded text edits** are fine only where no catalog address changes (table below). With
   `.screenplay/identities.json`, adding, removing or renaming a declaration, a property of a
   command, event, read model, composite type or trigger, a query, a query argument or a
   specification changes addresses; creating, deleting, renaming or moving a mapped `.play` file
   changes the document mappings (`IdentityMappingConflict` at reopen).
   A text edit is **never** a way around a refusal: if a typed operation refused,
   change the request or ask, do not retype the file.
4. **`id` pins alone are not enough when `.screenplay/identities.json` exists.**
   A text rename plus an `id "<old name>"` pin preserves authored stored-name intent
   but leaves the identity catalog pointing at the old declaration; the next
   `apply` can mint new identities silently. Use the identity-preserving operation.
   Differing event pins carry `storedName` in ESM v16;
   that does not repair a stale catalog or admit the model to a renderer that stops before
   v16. Evidence and target limits: `mcp-tools.md`.
5. **A session without MCP** (a subagent, or a harness with no server) carries out a
   requested rename by text with `id "<Old>"` pins and an identity note when no
   `.screenplay/identities.json` exists. When it exists, it does not edit
   catalog-changing declarations by text: it returns the edit request above to the
   owning session, and its report says first that the requested change is NOT done.

| Edit class | Examples | Route |
| --- | --- | --- |
| Address-preserving | descriptions; bodies of rules and expressions; mappings between members that already exist; any edit that adds, removes or renames no addressed element. Also every edit in a model with no `.screenplay/identities.json` except persisted names | bounded text edit of the `.play` file, then compile the folder; or `propose-ast` |
| Catalog-changing (identity file present) | add, remove or rename a declaration (slice, command, event, read model, projection, ...), a property of a command, event, read model, composite type or trigger, a query, a query argument or a specification; create, delete, rename or move a mapped `.play` document (McpState refuses reopening with `IdentityMappingConflict`; use document operations in `propose-ast`); rename, remove or move an event, command, read model, constraint, module, feature or slice; move declarations between files (slices and features across logical parents, and single declarations between slices: `propose-move`); contract evolution of a persisted event (new generation, property added, removed or retyped) | `propose-rename` or a reviewed `propose-ast`; never a blind text rename. Renaming a constraint resets its uniqueness index: ask first unless the request names it, then report the reset. A persisted event rename needs `id "OldName"` (added by default) |
| Concept `generate` | set/change/remove strategy and modifiers | `propose-ast` replaces `ConceptSyntax.generation` (`GenerationSyntax`; omit/null to remove), or reviewed `propose-source`; never hand-edit `generate` text. Preserve other concept members and inspect executable diagnostics: sequence PLAY0682, generated secrets PLAY0268, generated-rule proofs PLAY0688/0689, finite-support failures PLAY0691/0692. Pattern/random/rule and checksum validation bind as ESM v21 (Requires Screenplay 4.172.0, `screenplay --version`; older compilers refuse PLAY0268). Read `mcp-tools.md#concept-generation` for `arguments`, exact support and `rules[]`, not parsed diagnostic prose. |
| Repair | `PLAY0166`, `0478`, `0469`, `0471`, `0479`, `0516`, `0563`, `0564`, `0565`, `0614`, `0653`, `0683`, `0684` (and recipe-only `0397`); `PLAY0470` only with author intent ([destination intent repair](destination-intent-repair.md)) | `propose-repair` with `formatting`; PLAY0166/0478 may pin attachment evidence (`pinRepairEvidence` and `expectedRepairEvidenceRevision`), never fix-all PLAY0478 |
| Inline event extraction | `produces event ...` to a declared event | `propose-extract-inline-event` |
| Parser-invalid document | no editable handles | `propose-source` `replace-document` with corrected source, or typed `propose-ast` document replacement |
| Unadmitted constructs | systems/operations, the `numbers exact` source mode | reviewed Authoring `propose-source` (whole documents) or `propose-ast` (typed edits); preserve intent and report PLAY0268 |
| Admitted source/stream routing | ESM v8 | typed `propose-ast`; `propose-rename` repairs proven route references; target admission separate |

PLAY0683/PLAY0684 repairs raise NanoID size or bytes count to at least 64 random
bits using the concept subject (possibly across documents). PLAY0683 can instead
add uniqueness when exactly one production maps the command property to exactly
one event property: use the command-property subject. Discover `repair-capabilities`
and `read-workspace view: "repairs"`, then `propose-repair` with both revisions and
`CanonicalizeTouchedDocuments`; review every changed document before apply.
Neither action supports fix-all or pinned evidence. A concept change affects all
uses; uniqueness changes append-time behavior. Derived regex, matches/checksum
repairs and rule-proof status await phase 2b-2; details and source citations are in
[concept generation](mcp-tools.md#concept-generation).

## Reference continuity and located conflicts

Requires Screenplay 4.211.0 (`screenplay --version`) for independent reference-edit batches and string document/path fields; older servers can refuse independent remove/add batches and emit object-shaped fields. Upgrade before relying on this contract.

Batch independent reference removals and additions with different text in one
proposal, just as in sequential proposals. For example, removing a constraint's
`OrderRecalled` reference and adding a specification's `OrderLost` reference is
accepted when the remaining checks pass. Occurrence identity follows exact
correspondence, verified relocation pairing or trusted `propose-move` lineage;
unmatched references are removals/additions, not inferred moves.

Unmatched removed and added references with exactly the same text refuse as
ambiguous, even across owners/documents and under `Draft`. Comparison is ordinal:
qualification and case are not normalized; equal tail names alone are not identity.
Preserve a moved reference with `propose-move` or explicit semantic migrations.
`Safe` still refuses new unresolved references; matched references cannot lose a
resolved target, silently rebind unchanged text or capture existing debt.

On refusal, read `conflicts[].document` (document identity), `path` and `otherPath`
as plain strings when present, not objects with nested values. A conflict without
a document reports null. The message locates the reference text, member/role,
portable document path and line:column; ambiguous batches list disappeared and
introduced occurrences, capped at ten with a remaining count. Use these locations
instead of bisecting the batch. `candidates` stays empty for reference-validator
refusals; it is reserved for property-planner evidence. `propose-move` report
document fields are strings too.

## Self-describing identity migration refusals

Read `identityMigrationIssues` on rejected proposals
(conflict `InvalidIdentityMigration`). Each entry names typed `address.kind` and
`address.parts`, and the input `arguments` it belongs in: `semanticRenames`,
`eventRenames`, `retiredSemanticAddresses`, `retiredEventAddresses`. Stale renames
name both endpoints; missing continuity lists rename/retirement alternatives,
not an instruction to retire. Include both event-contract and semantic assignments
and all assigned descendants. Rename/apply messages name the same offending addresses.
Supply exactly the entries required for the intended change instead of retrying
by trial and error. Re-read actual revisions when stale; never clear the catalog.
Authority: `mcp/reference.md`, `mcp/authoring-tools.md`.

Requires Screenplay 4.89.0 or later (`screenplay --version`) for `identityMigrationIssues`.

## Identity state (`.screenplay/identities.json`)

- `apply` persists identities with source or notes-only changes; they survive
  restarts without `export-workspace`. `open-workspace` and read-only commands
  never create the file. Commit identity state with the model and notes.
- Containers anchor before binding when all documents parse and place; inspect
  `containerIdentities` in `open-workspace` / `workspace-state`. Fix binding
  through MCP proposals, not external edits after persisting a partial catalog.
  Read [state and recovery](state-and-recovery.md) for the exact unbound reopen
  exception and [model notes](model-notes.md) for targeting refusal reasons.
- A model only ever edited as text has **no persisted identities** until the first
  `apply`. Whether a first apply is the intended initialization is not documented
  beyond "after the first apply, keep `.screenplay/identities.json`"; ask before
  relying on it.
- Text-editing an identity-affecting declaration in a model that has the file
  breaks continuity silently on the next apply. In a model without it the risk is
  to persisted Chronicle event types (use `id` pins).
- Do not read the file for content (token cost); `read-workspace` with `view:
  "semantics"` returns identity assignments, not a model summary.
- See "Make one coherent proposal" in SKILL.md for the exclusive-write prerequisite of `apply`.

### Root discovery conflicts

Default discovery preserves `.screenplay/identities.json`
or `pending.json` on the ancestor-or-self path from the offered project to the
discovered model. Empty metadata folders/backups do not count. With several
state roots, the nearest the offered project wins. `rootBindingConflict` in
`open-workspace`/`workspace-state` contains `kind: "WorkspaceRootConflict"`,
`boundRoot`, ordered `stateRoots` and `pendingRoots`; it is omitted without a
conflict. A journal anywhere on that path blocks bound-root reads/writes too.

To recover a competing journal, explicitly `open-workspace` on its root (which
may return `PendingOperation`), inspect `workspace-state` there, then recover
that operation within the approved scope. Recovery at the outer root cannot
recover a nested journal. Never delete metadata, migrate identities implicitly
or create a fallback folder to bypass the conflict.

## Read-only views worth knowing

- **`source-map`** (`read-workspace`): the compiler's semantic entries ordered by
  semantic id with `role` (Declaration or Description), identity `origin`,
  `documentId`, `path` and an exact zero-based UTF-16 `span` plus one-based
  line and column. `available` is true only when compilation succeeded; an empty
  page alone is not evidence of success. Executable diagnostics have their own view.
- **`whenAppendedEvent`**: specification fixtures that use `when append` appear in
  `declaration-details`, the syntax index and `find-fixtures` with the role
  `whenAppendedEvent` (payload) and `whenAppendedEventDestination` (the `for`
  value). References and dependencies carry that role instead of `thenEvent`, so a
  query for `then` events does not return the appended event.
- **`executable-model`**: canonical ESM bytes; `modelRevision` (`rev1:<sha256>`)
  exists only when the model binds. Without an identities file it changes when the root folder is
  renamed (a relocation that keeps the name does not), because application identity is
  bootstrapped from the root name. Source positions and descriptions do not change it, and a
  file move changes it only if it changes logical placement or application identity. Do not compare it with a render manifest's revision. Do not read
  the bytes themselves.
- **`implementation-requirements`**: attachments with `bodySpan`/`bodyLines`; see
  the [MCP tool guide](mcp-tools.md#code-attachments).

## Failures (use `failureKind`, never message text)

| failureKind | Response |
| --- | --- |
| `StaleRevision`, `DiskDrift`, `IdentityStateDrift` | Reopen and re-propose |
| `ProposalRejected` | Read `conflicts`; change the request |
| `FormattingConsentRequired` | Pass `formatting` |
| `UnknownProposal` | The connection was reopened or the proposal applied/discarded: re-propose |
| `LimitExceeded` | Discard unused proposals (16 cap) |
| `PendingOperation`, `RecoveryRequired` | `workspace-state`, then ask before `recover-workspace` |
| `RootChangeRefused` | Keep the fixed root, or start a new connection |
| `ApplyOutcomeUnknown`, or EOF during `apply` | **Never retry `apply`.** Reconnect, read `workspace-state`, ask the user |

Never delete `.screenplay/pending.json`. Limits reject and never truncate: 512
files, 8 MiB total, 2 MiB per file, 1 MiB structured result, 200 items or 192 KiB
per page. Excluded directories: `.git`, `.ai-work`, `.screenplay`, `bin`, `obj`,
`node_modules`.

## Formatting and limits

Formatting is explicit. Default hand-written `propose-source` edits to
`PreserveExactSource`: supplied comments, alignment, blank lines and line endings
(including CRLF) survive structural edits; replacements retain the original BOM
policy. The same parsing, identity, reference and compilation checks run, without
PLAY0288. Choose `CanonicalizeTouchedDocuments` only when canonical output is
wanted. `propose-ast` exact mode is unchanged: `PreserveTrivia` rewrites supported
value/mapping spans and refuses structural changes; structural AST edits need
canonical printing. Its refusal points source authors to `PreserveExactSource`.

Requires Screenplay 4.158.0 or later (`screenplay --version`) for exact structural source proposals; older servers refuse that mode.
Requires Screenplay 4.156.0 or later (`screenplay --version`) for proposal `summary` and `authoringDiagnosticSeverities`; older servers lack these review fields.

 Before applying, check the proposal's `droppedCommentCount`; when it
is not zero, read `read-proposal` with `view: "dropped-comments"` and disclose
each lost comment (`PLAY0288` also reports their count and lines). Untouched
documents retain exact bytes. A printer that loses requested structural fields
rejects the plan.

## Provenance

The structure of this loop (ownership, edit-request shape, failure handling) is
adapted from the project's earlier event-modeling toolkit draft.
