---
name: cratis-application-slice-conformance
description: "Reconcile a Cratis slice's hand-written or gap-filled code and specifications with its contract (the `.play` slice and its specifications, else the agreed slice outline) in both directions: every contracted field, rule, event, read-model property and specification has a realization, and nothing in code lacks authority. Use when implementing a slice, re-delivering after a model change, finishing gap-fill after a render, or reviewing slice code. Not for: deciding what the slice should do (use `cratis-chronicle-event-modeling` or `cratis-screenplay-slice-design`), writing the specifications themselves (use `cratis-application-slice-specifications`), or rendering (use `cratis-screenplay-render-and-gap-fill`)."
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-application-slice-conformance/SKILL.md -->

# Slice conformance: the code matches its contract, element by element

## Purpose

Hand-written code drifts from what was agreed in two directions: something agreed is
missing, or something nobody agreed is there. This skill is the check that closes both, plus
the rules for specifications, re-delivery and the final status. It does not decide what a
slice should do; the contract does.

Model-first rule: when a `.play` model covers the slice, the model is the contract and
the code is derived. Code is written only for scope the renderer rejects, adapters and
other infrastructure. Change behaviour in the model first, never in managed output.

## When to use / when not

- Use: implementing or finishing a slice by hand; gap-fill after a render; re-delivery after a
  model or outline change; reviewing slice code; a hand-over where "done" must mean something.
- Not for: the choice of behaviour (`cratis-screenplay-slice-design`,
  `cratis-chronicle-event-modeling`); how to write specifications
  (`cratis-application-slice-specifications`); the render workflow itself
  (`cratis-screenplay-render-and-gap-fill`).

## Startup verification for Automation and Translation

- **Real startup participation.** A `ReactorScenario` bypasses observer registration, so it
  cannot show that the application subscribes the reactor. Verify, through the repository's
  integration route or a startup check, that the real application discovers the intended
  reactor (its assembly is scanned, per the repository's discovery convention), that it observes
  the intended event store and event sequence, and that a trigger event delivered there reaches
  it. Report this apart from the in-process specifications.

## Product facts

This skill is a method: it names no API of its own. When the Screenplay skills are installed their `SKILL.md`
(in `cratis-screenplay-toolchain`) explains how to read the installed tool versions.

| Fact used here | Source |
| --- | --- |
| Where each rule lives (validator, `Provide()`, handler result, constraint, exception) | `rules/vertical-slices.md` "The decision matrix"  |
| A customization never makes a rejected model renderable; managed output is never hand-edited | Stage (`Customizations/` is the unmanaged seam; managed output is regenerated) |
| Attached file or inline command handlers are refused with PLAY0800, a permanent requirement for a code implementation attachment (Requires Screenplay 4.210.0; older binders report PLAY0268); pending handlers with unconditional productions bind v23 as described commands, but reached bodies are Unsupported ImplementationBody and Stage refuses STAGE-IMPL-002 even in Stub mode | Screenplay and Stage; use the model as the gap-fill contract |

## Procedure

### 1. Name the contract and its precedence
Find the contract before touching code; say which one you are using. These steps are enough on
their own when the Screenplay skills are not installed; the master decision rule, when they
are, is `cratis-screenplay-modeling-lifecycle` ("Decide the level first").
Decide opt-in per `general.md` "Model first"; where `cratis-screenplay-modeling-lifecycle` is installed, run its `scripts/model-first-status.mjs` for the decision, model root and drafts.
Staged or untracked `.play` files are drafts, not the contract; a `.play` outside the model root is not a contract.
For a committed model with working-tree edits, HEAD remains the contract until the change is committed.
1. An accepted `.play` model under the model root covers the slice (search
   `**/*.play` for the slice, command, event and read-model names): the `.play` slice and its
   specifications are the contract.
2. The repository is opted in but this
   scope has no model yet: new behaviour starts in discovery and slice design
   (`cratis-screenplay-discovery`, `cratis-screenplay-slice-design`); do not code first. If those
   skills are not installed, say so and do not author `.play` from memory.
3. Not opted in: never force a model. The agreed slice outline (fields, events, rules,
   scenarios), confirmed as before and recorded where the team tracks work, if anywhere, is the
   contract. Only the entry-point session proposes a model, at most once per session.
4. Existing code has no authority over the contract.

Precedence inside the contract. Executable parts of the contract beat prose: specifications,
mappings, constraints and authorization first; then the `description`, `documentation` and realization notes;
then issue text. If prose contradicts an executable part, that is a model defect: stop that
scope, return an **edit request** (address, change, reason) and do not pick a side silently.
Reading a question is not resolving it. Details: `references/contract-and-precedence.md`.

### 2. Inventory per element (a working list, not a deliverable)

| Slice type | List |
| --- | --- |
| State Change | each command property and its authorization; each event with its properties; each rule, with the mechanism that enforces it |
| State View | each read-model property and its source event; subscribed events; removal and update events; query cardinality, filters and caller scope |
| Automation / Translation | trigger event(s); each field of the produced command or event with its source (trigger, injected read model, contract mapping); filter conditions; repeat behaviour |

Slice-type checklists and the Chronicle runtime guarantees: `references/checklists.md`. The
standing build prompts for an implementer or an unattended loop (mandatory flow, one slice per
iteration, ownership, result packet, learning candidates): `references/build-prompts.md`.

### 3. Reconcile in both directions
- Contract to code: every listed element has a realization; list each gap.
- Code to contract: every business field, default, rule, filter or outcome in code has
  contract authority. Anything else is invented: remove it, or if it is a real need, propose it
  for the contract (the `.play` model when one covers the slice, otherwise the agreed outline)
  and stop that scope. Framework metadata (stream type, concurrency scope,
  `[OnceOnly]`) is infrastructure and never changes the domain contract.
- A rule that lives only in code but is modeled elsewhere is a defect, not a convenience.
- Unrelated contracts stay as they were. Identity-affecting changes (renames, moves, removal of
  persisted names) go to the identity owner (`cratis-screenplay-modeling-lifecycle`
  `references/identity-and-edits.md`); a non-owner returns an edit request.

### 4. Map specifications one to one
Each contract specification gets one executable specification whose name reads as that
specification's name; split it only if every assertion survives. Report the mapping
(contract specification, spec class, result). Rejections map to the rule they name: build the
command valid in every other respect so a neighbouring rule cannot make it pass. Use the
contract's example values except where uniqueness needs fresh ones. Code-derived cases the
contract lacks are extra, and reported as proposals for the contract (the `.play` model when
one covers the slice, otherwise the agreed outline).

**Never edit, skip or delete a specification derived from the contract to make code pass.**
Change the code, or return an edit request. Do not touch test files outside the slice unless
told to.

Where the model is a `.play` and the code is Arc, Marten or Critter Stack, `cratis screenplay
conform <model-root> --project <path>` (exit 0/1/2) adds structural drift
evidence: exact-address comparison of recovered declarations, never behavior or equivalence. It
runs MSBuild on the project (trusted clone only). It supplements, and never replaces, steps 2 to 4.

Requires cratis CLI 3.42.0 or later (`cratis --version`) for `cratis screenplay conform`.

### 5. Delta delivery
After a model or outline change, the work list is every element and specification that
changed since the last delivery, including a delivery of only added specifications. Then run
steps 3 and 4 over the whole slice once more, because a change often invalidates a neighbour.

### 6. Conflicting evidence
Contract against code, prose against a mapping, or a specification against its own
description: stop that scope, record both readings with their addresses, ask one specific
question or return an edit request. Finish unaffected scope only if it stands alone. Do not
build anyway. Template against repository: the repository's established pattern wins
over a skill's generic template; note the mismatch as a learning.

### 7. Status
End every delivery with `Status: done | partial | blocked`:

| Status | Meaning |
| --- | --- |
| `done` | everything maps, nothing invented, the slice's specifications pass, unrelated gates green |
| `partial` | listed gaps remain (each named); the rest conforms |
| `blocked` | one specific question or edit request is open; nothing guessed |

Worked reconciliation and report shape: `references/worked-example.md`.

## Gate

Before reporting `done`, all hold:
- The contract is named and its specification list is in hand.
- Every contracted element has a realization and nothing invented remains in code.
- Every contract specification maps to an executed spec; none was weakened, skipped or deleted
  to accommodate code; changes follow only an approved contract revision and keep unaffected
  assertions; skipped or unexecuted specs are listed.
- The runner and per-specification outcomes are reported apart from any integration result.
- Deliberate divergences are listed with who approved them; none is called equivalent.
- Unresolved conflicts are `blocked`, never absorbed.

Passing specs are the evidence; the inventory is an aid, never a receipt. A slice is only
`done` if the business logic is implemented as the contract defines it, every scenario in the
contract is implemented in code, and no specification lacks an executable equivalent.

## Verify

- Run the slice's own specifications with the repository's runner and report the real result.
- Re-read the inventory against the code once more, looking only for invented content.
- For a `.play` contract, run the repository's Screenplay model check (the compiler named in
  `cratis-screenplay-toolchain` when installed) after any edit request is applied, never before.

## Route near misses

- What should the slice do or which events exist: `cratis-chronicle-event-modeling`,
  `cratis-screenplay-slice-design`.
- How to write or place specifications: `cratis-application-slice-specifications`,
  `cratis-chronicle-read-model-specifications`, `cratis-specification-by-example`.
- Which scenarios the model itself must have: `cratis-screenplay-scenario-coverage`.
- Rendering, rejections and gap-fill workflow: `cratis-screenplay-render-and-gap-fill`,
  `cratis-stage-rendering-and-sandbox`.
- Where a business rule lives in code: `cratis-arc-command-validation`, `cratis-arc-command`.
- General review of a diff: `cratis-code-review`.

## References (load on demand)

- `references/contract-and-precedence.md` - sources, precedence, conflicting evidence.
- `references/build-prompts.md` - standing instructions, mandatory build flow, loop prompt, result packet, learning candidates.
- `references/checklists.md` - State Change, State View, Automation final checklists; Chronicle runtime guarantees; evidence.
- `references/worked-example.md` - a marina slice reconciled and re-delivered, with the report. Read when you need an example of reconciliation, delta delivery, or the final report.
- `references/provenance.md` - sources and attribution. Read when checking source attribution or adaptation history.

## Lineage

Merges the fallback-conformance checklist of the render workflow with the final-verification
checklists and build prompts of Martin Dilger and Nebulit GmbH's slice build kit, adapted closely; see
`references/provenance.md`.
