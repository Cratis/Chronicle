<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/model-notes.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Model notes: questions that travel with the model

## Contents

- Verified product sources
- Choose the artifact
- Anchors and independent state
- List with bounded, pinned reads
- Propose, review, apply
- Resolve by evidence, then carry the answer into the model
- Source proposals can orphan notes
- Question gate is independent of compilation

## Verified product sources

All notes claims below use **Screenplay v4.144.0** (decision 0045, PR #683), not
Cratis CLI's older bundle. Sources in `Cratis/Screenplay` at that tag:

| Contract | Path and lines |
| --- | --- |
| Purpose, anchors, decisions and privacy | `Documentation/screenplay/model-notes.md:6-77`; `decisions/0045-model-notes-sidecar.md:40-79` |
| Filters, operations, review and orphan disclosure | `Documentation/screenplay/mcp/reference.md:457-520`; `Source/DotNET/Screenplay.Mcp/McpNotesSchemas.cs:10-83` |
| Retained proposals, revision guards and notes review | `Source/DotNET/Screenplay.Mcp/McpWorkspaces.Notes.cs:12-92,111-119` |
| Operation fields and resolution evidence | `Source/DotNET/Screenplay.Mcp/McpNotesOperations.cs:12-99` |
| Sidecar bytes, absence and canonical revision | `Source/DotNET/Screenplay/Workspaces/ModelNotesStore.cs:22-32,56-126` |
| Standalone question gate | `Source/DotNET/Tool/ModelOpenQuestions.cs:18-69,87-131`; `Documentation/screenplay/model-notes.md:93-121` |

The source-edit and authoring guides remain useful for the ordinary proposal loop:
`Documentation/screenplay/mcp/edit.md:201-226` and `authoring-tools.md:16-40`.
The notes-specific arguments are in the reference and sources above.

## Choose the artifact

Record an unresolved modeling choice as a `question` note on the affected element,
not only in chat, session STATE.md or `description` text. Include the consequence
and the assumption in use, or say the scope is blocked. Use `task` for work to do
and `note` for review context. All three can be open or resolved.

A description states meaning, documentation explains settled reasoning, and a
project-owned decision record preserves a consequential choice. Notes link to
these artifacts; they do not implement behavior or replace decision records.
Cite the project's own decision identifiers in `refs`, for example
`[{ "id": "D123", "url": "https://example.org/decisions/D123" }]`.
URLs must be absolute HTTP(S); the server never fetches them. Read the actual
project record before treating its identifier as evidence of an answer.

**Never put secrets or personal data in note text, author, references or
resolutions.** The sidecar is committed. Use invented examples and team roles or
handles, not personal names. `author` is unverified free text, not authentication
or authorization.

## Anchors and independent state

Notes live beside the model in committed `.screenplay/notes.json`. An anchor is
`{ semanticId }` or `{ application: true }`. Fetch IDs from the current catalog;
do not derive them from names. Renames and moves keep notes without rewriting the
sidecar. `lastAddress` is a display hint, never an anchor.

For a syntax-only construct, select its current source handle or address and kind.
The server substitutes its nearest identified slice, feature, module or application;
inspect `substitutions` and name the particular construct in the note text.
Document and authoring-key anchors are unsupported.

Notes do not change source, workspace/catalog/semantic revisions, executable
model bytes or compiler verdicts. Their own `notesRevision` is `nrev1:` plus the
lowercase SHA-256 of canonical bytes; `nrev1:absent` is not an empty document.
The file header uses `schema: "cratis.screenplay.notes"`, `schemaVersion: 1` and
`applicationIdentity` in the catalog's representation. The server writes stable
key order, LF plus a trailing newline, and one note object per line sorted by ID.
Commit it with the model; never hand-author a sidecar to bypass proposals.

## List with bounded, pinned reads

Open the workspace, inspect `notes` status and retain workspace/catalog revisions.
`list-model-notes` returns `notesRevision`, `view` and `page` with `revision`,
`totalCount`, `offset`, `items`, `nextOffset`.

| Filter | Meaning |
| --- | --- |
| `anchor` | `{ semanticId }`, including an orphan ID, or `{ address, kind }` |
| `scope` | Dotted module/feature/slice; descendants and ancestor notes |
| `kind` | `question`, `task`, `note`; omitted selects all |
| `state` | `open` by default; `resolved`, `orphaned`, `all` |
| `ref` | Exact identifier in `refs` or `resolution.ref`, e.g. `D123` |
| `view` | `items` by default, or `findings` |

Use `offset`/`limit` (at most 200, also byte-bounded); continue from `nextOffset`
and echo `notesRevision` as `expectedNotesRevision` after the first page.
Restart rather than combine pages after drift.

Items carry the durable `note`, current `address`, computed `state` and nullable
`orphanReason`. `view: "findings"` returns one info finding per selected open
question with `code: "model-note.open-question"`, `noteId`, message and owning
slice/container address. These are MCP/CLI-only findings, **no PLAY code** and
no compiler warning. Use `state: "all"` for findings including orphaned open
questions; the default open filter does not prove there are no orphans.

## Propose, review, apply

Only the connection owner proposes/applies. `propose-model-notes` requires
`expectedRevision`, `expectedCatalogRevision`, `expectedNotesRevision` and 1–200
`operations`. It retains an atomic batch without writing; a failed batch retains
nothing. These are `operation` values, not an `action` parameter:

| Operation | Additional fields |
| --- | --- |
| `add` | `kind`, `text`, `author`, exactly one `anchor` or `target`; optional `refs` |
| `edit` | `id`; optional `kind`, `text`, `refs` (replaces the whole list) |
| `resolve` | `id`, `resolution` with `by`, `text`, `author`; evidence below |
| `reopen` | `id`; clears resolution |
| `reanchor` | `id`, exactly one `anchor` or `target` |
| `remove` | `id`; explicitly deletes the note, not how to answer a question |

`target` accepts a dotted `{ address, kind }`, typed address, or current
`{ revision, documentId, path }` source handle. IDs and UTC instants are
server-generated; do not supply `createdAt` or resolution `at`. Text and author
are trimmed, nonempty and at most 16384 characters; at most 100 refs per note,
10000 notes and 4 MiB UTF-8 per sidecar.

Example **argument template** for `propose-model-notes`: substitute the three
revisions just read and an actual declaration address before calling it.

```json
{
  "expectedRevision": "<workspace revision>",
  "expectedCatalogRevision": "<catalog revision>",
  "expectedNotesRevision": "<notes revision>",
  "operations": [{
    "operation": "add",
    "kind": "question",
    "target": { "address": "Library.Loans.CheckIn", "kind": "Slice" },
    "text": "Can a member cancel after check-in? This changes cancellation rules; assuming cancellation is blocked until answered.",
    "author": "modeling-team",
    "refs": [{ "id": "D123" }]
  }]
}
```

1. Retain `proposalId`, `afterNotesRevision`, server `addedNotes` IDs and inspect
   `substitutions`. `changeCount: 0` means no source changes, not no notes change.
2. Read `read-proposal` with `view: "notes"`: before/after notes revisions and
   paged `{ id, before, after }` pairs. On continuation pin **afterNotesRevision**
   as `expectedNotesRevision`, not the disk revision. Review the entire batch.
3. `apply` the reviewed server proposal with its ID and before workspace/catalog
   revisions under the ordinary authorization contract. Apply verifies notes
   revision and exact original bytes; no extra notes parameter on apply. It
   journals source/identity/notes effects under the existing `pending.json`.
4. Read fresh state after apply before another proposal. For `NotesRevisionDrift`,
   reread notes and re-propose; for `StaleRevision`, reopen and re-propose. An
   unknown apply outcome follows the ordinary recovery loop, never blind retry.

Malformed notes do not block opening the model. Conflict markers, duplicate IDs,
invalid schema/version or another application's identity report `notes.status:
"unreadable"`; note tools refuse `UnreadableNotes` without auto-repair. Restore
known-good content or deliberately resolve the conflict, then read again. Never
interpret unreadable state as an empty notes set.

## Resolve by evidence, then carry the answer into the model

A null resolution means open. A `resolve` operation's `resolution` contains:

| `by` | Evidence |
| --- | --- |
| `specification` | Required existing `semanticId` of the specification |
| `description` | Required existing `semanticId` of the described element |
| `decision` | Recorded modeling decision; optional `semanticId` or `ref` |
| `external-decision` | Required `ref`, e.g. `{ "id": "D123" }` |
| `dismissed` | Explanation why it no longer applies |

For D123, send `operation: "resolve"`, the returned note `id`, and
`resolution: { "by": "external-decision", "ref": { "id": "D123" },
"text": "D123 disallows cancellation after check-in.", "author": "modeling-team" }`.
Read the proposal's notes view and apply; use `list-model-notes ref=D123
state=all` to find both citations and resolutions.

Resolving does not implement the answer. Turn a resolved question into a
specification for behavior, a description for clarified meaning, or a
project-owned decision record for consequential policy. Apply a changed model
and its specifications through a separate source proposal, run the appropriate
verdicts, then resolve with the resulting evidence. A known refusal belongs in
`then denied` or `then error`, not just a resolved note. Reopen if the answer
stops being adequate.

## Source proposals can orphan notes

Review `orphanedNotes` IDs and `notes` status/reason/count on source, AST, rename,
move and property proposals and their reviews. Retirement leaves a computed
orphan with null current address and `orphanReason: "unknown"`; notes are never
silently deleted. `orphanedNotes: null` with unreadable notes means unknown
impact, not `[]`.

Before applying a source proposal, review each affected question: reanchor to a
surviving element, or resolve with evidence and deliberately decide whether to
retain its orphaned history. Resolution alone does not restore an anchor. Remove
only when explicitly intended. A separate notes apply invalidates the old source
proposal; reopen and re-propose the source change, then inspect orphan impact
again before apply. A read-only reviewer sends note/edit requests to the owner
and never writes the sidecar.

## Question gate is independent of compilation

Read toolchain [versions](../../cratis-screenplay-toolchain/references/versions.md#model-notes-gate)
when choosing the CLI gate. **Standalone Screenplay 4.144.0 has it; Cratis CLI
3.53.1 does not.** `--warnaserror` alone ignores notes. Tasks and ordinary notes
do not fail the question gate; resolving questions is not V1–V5 evidence.
