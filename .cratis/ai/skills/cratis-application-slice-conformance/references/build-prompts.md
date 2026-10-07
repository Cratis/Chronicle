<!-- cratis-ai-managed: skills/cratis-application-slice-conformance/references/build-prompts.md -->
# Slice build prompts

Standing instructions for an implementer, or an unattended loop, that builds one slice at a
time from its contract. Paste or reference them in the brief; they restate rules already in
`SKILL.md` as an operating procedure. Adapted from Martin Dilger and Nebulit GmbH's build kit (see
`references/provenance.md`); the board, `slice.json` and status-API steps are translated to
the `.play` slice (or the agreed outline) and the code skills. They sit inside the existing
authority, gate and ownership rules: the contract precedence in
`references/contract-and-precedence.md`, the repository's full completion gate, and the
coordinator's ownership of status, git and shared notes.

## 1. Standing project instructions

- Read the shared event vocabulary first (the `.play` events of the module, or the `[EventType]`
  records in the repository) to understand the global structure.
- **Owned paths.** Edit only the slice's own folder (`<Module>/<Feature>/<Slice>/`, backend,
  frontend and specifications together) and any path the brief names. Do not browse unrelated
  slices. Bounded reads of what the contract references are allowed and expected: the events,
  views, concepts and inherited authorization the slice names, and shared contracts it depends
  on. Leave routing and registration files alone unless the brief names them.
- Each slice is self-contained and focused on one behavior; keep concerns separated inside it.
- Slice names match case-insensitively in a prompt (`ReserveBerth` is `reserveberth`); resolve
  the qualified address (`Module/Feature/Slice`) before touching anything.
- **Do not change existing specification files unless explicitly instructed.** A specification
  derived from the contract is the oracle; the instruction to edit one comes from an approved
  contract revision, never from a failing run.
- Read the repository's own conventions file (its `AGENTS.md` or equivalent) at the start.
  Nothing here creates or edits shared notes.
- Status and ownership are the coordinator's: a slice is worked on only when the brief assigns it
  to you. Reporting is by the result packet (section 4), not by editing a tracker.

## 2. Build a slice: the mandatory flow

**Always build through the matching slice-type skills; never freehand a slice.** All fields,
event names, command names and business rules come exclusively from the contract. Do not
invent, assume or guess a field or a rule that is not in it.

1. Read the slice from the contract in full: description, realization notes, specifications,
   concepts, inherited authorization, referenced events and views.
2. Determine the slice type:
   - `Translate` (a `capture`, or a translator `reaction` over an imported or captured event):
     outside data becomes our events. Realize by the actual construct and trigger, not the slice
     label: a capture is an ingestion adapter behind an effect boundary
     (`cratis-engineering-effect-boundaries`); a translator reaction on an event is a reactor
     (`cratis-chronicle-reactor`); a clock or application trigger is a scheduler or host signal.
   - `Automation` (a `reaction`): an event trigger is a Chronicle reactor
     (`cratis-chronicle-reactor`); a clock or application trigger is a scheduler or host signal
     (`cratis-engineering-effect-boundaries`); the produced command or event follows
     `cratis-arc-command`.
   - `StateView` (a `readmodel` with a projection and queries): `cratis-chronicle-read-model`,
     `cratis-chronicle-projection`.
   - `StateChange` (the default: a `command` that `produces` events): `cratis-arc-command`,
     `cratis-arc-command-validation`.
3. Invoke the matching skill and follow it completely; do not deviate.
4. Verify against the contract (`SKILL.md` step 3 and `references/checklists.md`): every
   command field, event field, read-model property and specification appears in the
   implementation. No invented fields: if it is not in the contract, it is not in the code.
5. **Iteration checks:** build the affected project and run the slice's own specifications while
   you work.
6. **Completion gate:** before reporting `done`, run what the repository's CI-equivalent gate
   requires (its Tier 1 checks, plus the integration evidence for the behavior claimed;
   `cratis-screenplay-render-and-gap-fill` `references/gap-fill-handoff.md`, "What comes
   back"). Slice-only runs are never the completion gate; an unavailable check is
   `not run: <reason>`.
7. Git effects (commit, merge, push) happen only when the brief separately authorizes them, and
   a commit contains only the files you own. Otherwise leave the changes in the working tree and
   return the result.

## 3. One slice per iteration (loop prompt)

For an unattended loop over a list of slices. A person following the same steps interactively
needs only the rules.

**Scope boundary.** Work within exactly one scope at a time: the module or feature the brief
names. Edit nothing in another scope; reads follow section 1. A scope with nothing left to do
ends the iteration; the brief, never the agent, changes scope.

1. Read the repository's conventions, then the brief's slice list; each entry assigned for
   delivery is a task.
2. Take the highest-priority slice the coordinator marked ready for delivery. Never pick up one
   that is in progress elsewhere, done, blocked or still being modeled, even if it looks
   incomplete.
3. **Ownership.** The coordinator assigns each slice to exactly one implementer. Setting a
   status or the lifecycle's `STATE.md` `Active:` line is a record, not a lock: it is not atomic
   and two workers can both write it. Rely on atomic claiming only where a tracker provides a
   verified compare-and-set operation; there, a rejected claim means another worker owns the
   slice: do not retry it, take the next one assigned to you, or report that nothing remains.
4. Work on one slice only; never chain two in one iteration. Under a ledger brief (one assigned
   ledger scope, which may hold several slices), take one slice at a time within that scope and
   never select another scope; one slice per run stays the default outside a ledger brief.
5. Read the slice's contract. Explicit realization requirements in the description bind the
   implementation, cannot contradict executable parts and are never supplemented with inferred
   rules; descriptive prose that is not an explicit requirement is a hint. Name each requirement
   you use in the result.
6. A delivery can be just added specifications: always read the slice and its specifications
   together, and any specification present in the contract but absent in code must be added
   (`SKILL.md` step 5, delta delivery).
7. Make a todo list of what needs doing, then carefully compare events, fields and
   specifications against the implementation.
8. The contract is always true; the code follows what it defines. The slice is `done` only
   if the business logic is implemented as defined, the APIs exist, every scenario in the
   contract is implemented in code and the slice fulfills the contract. **There must be no
   specification in the contract without an executable equivalent in code.**
9. Run the iteration checks, then the completion gate (section 2, steps 5 and 6).
10. Review the slice's specifications against `cratis-screenplay-scenario-coverage` when it is
    installed, and list any applicable scenario the contract lacks as a proposal for the
    contract (never add it as a second contract in code).
11. Report the result packet (section 4) and finish.

**Escalation.** If the requirements are genuinely ambiguous, contradictory or missing a
decision you need, do not guess and do not build anyway: return an edit request (or ask the one
specific question), report `blocked` and stop this iteration. This is an escalation path, not a
routine step; read the contract and the slice-type skills fully first, since most slices are
fully specified and need none.

Stop conditions: after one slice, stop regardless of what else is assigned (under a ledger brief,
after one slice, or when the assigned scope is done or blocked, and never continue into another
scope); if nothing in the
scope is assigned, report that and stop; if every slice in the scope is `done`, report that.

## 4. Result packet

Use the lifecycle's report and state format (`cratis-screenplay-modeling-lifecycle`
`references/handoff-template.md`, with its `STATE.md` under `.ai-work/`) when it is installed,
otherwise this shape. One packet per slice, returned to the coordinator, not appended per step:

```text
Outcome: <qualified slice address> - done | partial | blocked
What was implemented; files changed
Specification mapping: <contract specification -> spec class -> result>
Checks: <iteration checks and completion gate, each a result or "not run: <reason>">
Learning candidates (max 3; repository facts with evidence, or "none"):
- [area] <fact> - evidence: <command | file:line | doc>
```

## 5. Learning candidates

Return at most three per slice, each backed by evidence (a command result, a file and line, or a
document). Promotion into the repository's documentation or an issue is the main session's
decision; an implementer edits no shared notes and keeps no learnings file. Worth returning:
API patterns or conventions of a module, gotchas and non-obvious requirements, dependencies
between files, testing approaches for an area, configuration or environment requirements,
"when changing X also update Y".

Not worth returning: slice-specific implementation details, story-specific rules (a rule such
as "membership renewal requires an unpaid-invoice check" belongs in the contract, not in notes),
temporary debugging notes, or what the repository already documents. When a template and the
repository disagree, the repository's pattern wins and the mismatch is a candidate; where prose
in a description and an executable part disagree, the executable part wins
(`references/contract-and-precedence.md`), so a learning never resolves a contract conflict by
itself.
