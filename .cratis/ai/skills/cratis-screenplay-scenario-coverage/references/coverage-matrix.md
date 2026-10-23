<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/coverage-matrix.md -->
# Coverage matrix

## Contents

- Cell values
- Derive obligations from the model
- Template: commands, reactions, captures
- Template: read models
- Worked example (`berth-reservations.md`, design mode)

Write it **before** drafting specs, keep it in `.ai-work/screenplay/<model-slug>/coverage.md` (or the
`STATE.md` beside it), and update it with the specs. Reviewers check it against the model. Screenplay 4.127.0 MCP `find-specification-obligations` seeds the inventory (SPEC001–SPEC008), for command success, requirements, command/concept validation, effective authorization gates, value constraints, read models, projection removals and reaction outcomes. A met row proves matching authored presence, not full coverage or passing execution: the matrix still records domain judgments and scenario types the heuristic cannot infer.

For SPEC004, a gated query's denial can use `when query <Query>` with
`then denied`, or no `when` and exactly one `then query <Query>` with `arguments`
and no `result`, followed by `then denied`. The read-only form meets that query's
denial obligation, not the returned read model's SPEC006 assertion obligation.
Keep a separate view state, absence or query-result specification.
Requires Screenplay 4.212.2 (`screenplay --version`) for this inventory matching;
older servers miss the read-only denial for SPEC004 and count it for SPEC006.
Upgrade before gating on those rows; a met denial still proves presence, not execution.

## Cell values
- `spec <Name>` - written; the name exists in the model.
- `n/a: <domain reason>` - the situation cannot occur in this business. "Keeps it short" is not
  a reason.
- `recorded: <why>` - real, but the language cannot express it today (state-dependent rule,
  idempotent success, exactly-once effect, parallel race without a constraint). A state-dependent
  rule is always written as `reads <View>` + `require ... message` (binding then reports
  PLAY0268/0271; report the model as design-complete, not binding-ready) and marked
  NOT enforced in the slice `description`, with its target named (Arc `[ProtectedDecision]`,
  Chronicle DCB, or a constraint). It is also in the phase report.
- `n/a: technical` - deliberately unspecified because the language or runtime already
  guarantees it. The list is closed: binder-enforced shape (type, required property),
  collision of `generated` identifiers, and an infrastructure fault the runtime absorbs with
  nothing a person could notice. A repeat or re-delivery the business would notice (a second
  fact, email, charge or count) is never technical: it is Duplicate/retry (catalogue section 5).
  Anything else needs a domain reason or another cell value. Review treats a spec in these
  categories as a smell, not as coverage.
- `open: <required case>` - planned but not yet drafted. Never counts as coverage.
- `question: <id>` - needs a domain answer; listed in open questions.
- `gap: <code>` - expressible in design mode but blocked in the chosen mode (e.g. removal in
  renderable mode); the gap is recorded per `cratis-screenplay-modeling-lifecycle`.

## Derive obligations from the model

Before reading existing specs as evidence, list the obligations each declaration creates, then
map each to a spec whose body really exercises it. One spec may serve several compatible
obligations; a declaration alone never satisfies one.

| Model evidence | Required coverage decision |
|---|---|
| Command | success, and every distinct business outcome |
| Command rule (`validate`/`require`) | allowed boundary plus an isolated rejection with the intended message |
| Concept `validate` | one acceptance, rejection and boundary spec per concept, through one command; link it from other uses |
| Effective gate on a command or query (own, feature, module) | allowed caller plus a `then denied` at that exact command or query; fixtures derived from the Boolean gate: for AND isolate each missing conjunct, for OR a caller meeting any permitted alternative must succeed, `then denied` only where the whole gate is false (one per persona that may not act); for an ownership policy, a success with the matching claim and a denial with another value |
| `unique event` | the earlier constrained fact on the same source, then the repeat or the exclusive action |
| Value `unique ... on` | the value held by source A, a valid command on source B, the constraint message |
| `released by` | claim, release on its owning source, then a new claimant succeeds |
| `produces when` | feasible true and false witnesses, threshold equality, meaningful overlaps |
| Projection mapping | a view assertion for each changed value and for important preserved values |
| Repeated accumulating event | two occurrences when accumulation is business behaviour |
| `remove with` | existing row, removing event, explicit absence; a second surviving row |
| Query | caller-visible result for its contract; denial when gated |
| Reaction, capture, reversal, evolution, timing | the catalogue cases, or a specific reason, gap or question |

These are prompts, not claims that a declaration enforces the intended rule. A competing-claim
spec pins the loser's outcome after a prior claim; it does not run commands concurrently.
For a compound gate, a caller missing only one alternative of an OR gate must still succeed;
never change the policy to simplify fixtures. For conditional productions derive the full event set
per chosen input: unconditional productions plus every condition that is true. Do not invent
impossible combinations to fill a truth table.

**Vary one thing.** In each rejection spec change only the value the rule under test checks;
keep everything else (caller, other fields, history) identical to the success case, so the spec
cannot pass because a different rule fired.

**Completion.** A matrix with `open`, `question` or `gap` cells is an explicitly incomplete
scope; say so. Check commands and read models in two separate passes. A same-source retry is
not a competing claim, a denial elsewhere is not this command's denial, and removal from an
already-empty world is not removal coverage.

## Template: commands, reactions, captures
```text
Mode: <design|executable|renderable>   Model root: <path>   Source revision: <rev>

| Element | Happy | Rules | Denial | State | Duplicate/retry | Competing | Alt path | Ordering | External | Compensation | Evolution |
|---|---|---|---|---|---|---|---|---|---|---|---|
| <Cmd> | | | | | | | | | | | |
```
One Denial cell per persona that may not act (not for permitted personas), plus one ownership cell when the gate
has a claim policy. Add one row per gated query, not only per command.

## Template: read models
```text
| Read model | Family? (reason) | Population | Updates | Removal/absence | Ordering | Denial |
|---|---|---|---|---|---|---|
| <RM> | | | | | | |
```

## Worked example (`berth-reservations.md`, design mode)
| Element | Happy | Rules | Denial | State | Duplicate/retry | Competing | Alt path | Ordering | External | Compensation | Evolution |
|---|---|---|---|---|---|---|---|---|---|---|---|
| ReserveBerth | spec ReservingAGuestBerth | spec RejectingAZeroLength; spec RejectingAnOversizedBoat; spec RejectingANightInThePast; spec RejectingAnEmptyBerthCode (concept rule); spec ReservingABoatAtTheMaximumLength | spec RefusingASkipper | spec RejectingANightInThePast (declarative `night >= today`; binding blocked: PLAY0268) | spec RejectingARetriedReservation; recorded: retry should answer "already reserved" | spec RejectingASecondBoatForTheSameBerthNight | n/a: one outcome | n/a: single source | n/a: no external system | spec ReservingABerthNightFreedByACancellation | n/a: new contract |
| CancelReservation | spec CancellingAReservation | spec RejectingACancellationWithoutAReason | spec RefusingASkipperCancellation | spec RejectingACancellationAfterDeparture (live-reservation message); spec AppendingACancellationAfterDeparture (constraint, append time); recorded: live reservation required (`reads BerthBooking` + `require`; NOT enforced; target DCB or `[ProtectedDecision]`) | spec RejectingASecondCancellation (live-reservation message); spec AppendingASecondCancellation (constraint, append time) | n/a: one reservation, one office action | n/a: one outcome | n/a: single source | n/a: no external system | question Q1: can a cancelled reservation be reinstated? | n/a: new contract |
| RecordDeparture | spec RecordingACleanDeparture | n/a: input is only an id and a flag, no value rule | spec RefusingASkipperDeparture | spec RejectingADepartureAfterCancellation (live-reservation message); spec AppendingADepartureAfterCancellation (constraint, append time); recorded: live reservation required (`reads BerthBooking` + `require`; NOT enforced; target DCB or `[ProtectedDecision]`) | spec RejectingARepeatedDeparture (live-reservation message); spec AppendingARepeatedDeparture (constraint, append time) | n/a: one departure per reservation | spec RecordingACleanDeparture (condition false); spec RecordingADepartureWithDamage (condition true) | n/a: single source | n/a: no external system | question Q2: who clears a damage report? | n/a: new contract |

| Read model | Family? (reason) | Population | Updates | Removal/absence | Ordering | Denial |
|---|---|---|---|---|---|---|
| BerthBooking | yes: shared reserved prefix, then separate departure and cancellation branches | spec BerthBoardLifecycle1Reserved | spec BerthBoardLifecycle2Departed | spec BerthBoardLifecycle3Cancelled; spec RemovingOnlyTheCancelledBooking | n/a: single source | spec RefusingASkipperTheBoard |

Notes on the example:
- A constraint explains enforcement but does not replace an example of its outcome:
  `RecordDeparture` needs `AppendingARepeatedDeparture`; until it exists the cell is
  `open: repeated departure`.
- When a command's own validation rejects the same history first (here `require booking.status ==
  "reserved"`), a command specification can only pin that validation message. Test the constraint
  in isolation with `when append <Event>` (append-time constraints are checked there, Screenplay
  `specifications.md`); `when append` is not renderable, so keep it as a design or executable-mode
  spec.
- The numbered view specs share fixtures, not execution state. Departure and cancellation are
  sibling branches from the reserved state; cancellation does not follow departure.
- Stored-state rules stay in the model as `reads` + `require`; the example is design-complete
  and binding reports them. The past-date rule is declared in `validate` with its rejection
  spec; binding reports it too (PLAY0268, no runtime date value), so it is recorded as blocked,
  never moved into prose or deleted to reach V3 (binding-ready).
