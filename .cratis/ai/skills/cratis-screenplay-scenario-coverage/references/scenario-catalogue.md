<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/scenario-catalogue.md -->
# Scenario catalogue

## Contents

- Eliciting cases
- 1. Happy path
- 2. Rule rejection (one per rule)
- 3. Authorization denial
- 4. State violation
- 5. Duplicate action and retry
- 6. Competing claim (concurrency)
- 7. Alternative path
- 8. Ordering and timing
- 9. External failure
- 10. Compensation
- 11. Evolution compatibility
- 12. Views
- Example data
- Redelivery selectors
- Scenario types at a glance

For each type: the question to put to the domain, when it applies, when "n/a" is a legitimate
answer, the Screenplay shape, and traps. Excerpts point to the complete compiled example
`berth-reservations.md` (marina guest berths). An `n/a` must name a domain reason; a
`recorded` cell names what the language cannot express and where the rule is written down.

## Eliciting cases
Skip what the brief or model already answers. Otherwise ask:
- Which mistakes would be costly to get wrong?
- For this rule: one allowed request, the nearest refused one, and why they differ.
- What changes once the action has succeeded? What if the acknowledgement was lost?
- What if another identity already holds the same value?
- Which input flips the outcome, and what happens exactly at the boundary?
- Who may do or see this, and which otherwise-valid caller must be refused?
- What reverses the decision, and what must disappear, remain or become available afterwards?

Write each useful counterexample down at once as a candidate obligation. Business owners answer
intent, developers enforcement and failure boundaries, testers the distinguishing fixtures; record
who actually answered - an agent asking itself four kinds of question is not four approvals.
Unknown answers become questions, never invented rules. A quick pass may defer cases but must
name them as unfinished.

## 1. Happy path
- Ask: "When everything is in order, what is now true that was not before?"
- Applies: always, once per distinct success outcome.
- Shape: `when <Cmd>` with every input stated; `then` lists every production that fires, in
  order, with full payloads and `for`. See `ReservingAGuestBerth`.
- Traps: asserting a subset of events (lists are exact); leaving out `given caller` on a gated
  command; omitting `for` so the event lands on an allocated source.
- Generated values and responses (ESM v7, standalone Screenplay 4.68.0 and later; syntax in `cratis-screenplay-specifications`):
  when the command has a `generated` property, the happy path supplies its fixtures (`for` on the generated
  identifier, `generated <name> = <value>`); only when the command declares a response does it assert
  `then returns` (a return expectation without a response is rejected at compile time). Add the failing
  counterparts as separate specs: a rule rejection or denial (which happens before generation and has no
  response). A reached generated value without a fixture is `Unsupported(IdentityAllocation)`: the runner
  reports it as not passed, so it is a known capability limit to record, not a specification to write.
  A generated identifier is no retry identity (section 5).

## 2. Rule rejection (one per rule)
- Ask: "Which values would you refuse, and what would you tell the person?"
- Test: could a domain expert recognize the rule from its message and argue about it? If not,
  it is not a scenario. Not scenarios in Screenplay: a type mismatch the binder rejects, a
  missing required property, malformed JSON, a collision of `generated` identifiers, an
  infrastructure fault (matrix cell `n/a: technical`).
- Deliberate exceptions: a concept rule is domain vocabulary that renders into a validator at
  every use, so it gets its one acceptance, rejection and boundary set; this is not per-field
  schema testing. Every rule stated in the brief or a persona `Cannot` line maps to a matrix cell.
- Applies: every command `validate`/`require` rule that is a business rule, and every
  `concept` rule.
- Concept rules travel with every use: cover each once with representative acceptance,
  rejection and boundary specs through one command that carries the concept, not on every
  use. Never an automatic `n/a`.
- Shape: `then error "<the rule's message, verbatim>"` (prefer a `$strings.<key>` message where
  the model uses strings). The language accepts a bare `then error` (it matches any rejection),
  but this pack requires the pinned message: a bare error cannot show which rule fired. See
  `RejectingAZeroLength`, `RejectingAnOversizedBoat`.
- Vary only the value the rule checks; keep every other value as in the success case.
- Concept rule: one rejection spec per concept through one command (`RejectingAnEmptyBerthCode`).
  This matches `cratis-screenplay-specifications`, which also asks for one rejection spec per
  concept rule; the spec proves the rule travels with the concept.
- Traps: one spec covering two rules; `then error ""`; expecting `severity warning` to pass.

## 3. Authorization denial
- Ask: "Who can see this screen but must not do this?"
- Applies: every command or query with an effective gate, including module and feature gates:
  one `then denied` per command or query, not one per feature. Persona "Cannot" lines count only
  once they resolve to a gate plus this spec; otherwise record a gap.
- Shape: `given caller` that is authenticated but lacks the role or claim -> `then denied`
  (commands: no other outcome; read-only queries: `then query <Query>` with `arguments`, no
  `result`, then `then denied`; or `when query` + `then denied`). See `RefusingASkipper`.
  An empty `given caller` block is the anonymous caller.
- Use `given caller as <Persona>` for one authenticated witness synthesized from
  that persona's policies. Inspect the leftmost buildable `or` choice;
  it does not represent every possible caller. Use explicit roles/claims for
  anonymous or ownership fixtures, missing conjuncts, or refused synthesis.
  One denial per persona that can reach the screen but may not act. Derive
  fixtures from the effective Boolean gate: AND - isolate each
  missing conjunct; OR - a caller meeting any permitted alternative must succeed (a success
  spec, not `then denied`); denial only when the whole gate is false.
- Ownership policies (`claim ... matches subject|<property>`): the denial gives the right role
  and a claim with another value (`claim "ownerId" = "<other id>"`); the success spec gives the
  matching value. Both are needed. Ownership checks on text, Uuid, Date and DateTime subjects render;
  other subjects are STAGE-ESM-015: note it.
- Traps: `then error` for a denial; a denial spec whose inputs also break a rule (it still
  denies, but reads ambiguously); trusting a `cratis run` sandbox answer (the default engine enforces no authorization;
  the opt-in semantic engine enforces both but reads forgeable caller headers, so it is no security boundary); counting another command's denial.

## 4. State violation
- Ask: "After which earlier facts must this request be refused?"
- Applies: lifecycle rules (cannot cancel after departure, cannot pay twice).
- Expressible today: exclusivity of facts per event source (`unique event` lines sharing one
  constraint name), uniqueness of values across sources. See `CancelOrDepart` and
  `RejectingACancellationAfterDeparture` (command validation) and
  `AppendingACancellationAfterDeparture` (the constraint).
- Not enforced in the model today: rules over stored state such as "the reservation must exist"
  or "amount within the remaining balance". Write the rule as `reads <View>` + `require <expr>
  message "..."` (PLAY0268/0271 at binding are expected; report the model as
  design-complete but not binding-ready, never delete the rule to bind), mark it NOT enforced in the slice `description`, name its target (Arc
  `[ProtectedDecision]` + `DecisionRead<T>`, Chronicle DCB, or a constraint), mark the cell
  `recorded` and list it in the phase report. Forbidden: a caller-supplied copy of state in `require`,
  a boolean attestation input (`confirmsX == true`) standing in for the rule, and rules hidden in
  `handler` or implementation-hint prose. An unguarded materialized read is unsafe for a
  protected decision.

## 5. Duplicate action and retry
- Ask first as a business question: "What if someone does this again to something already in
  that state?" (confirm a confirmed reservation, resolve a resolved todo). Then take the
  delivery view: a double click, a timeout and retry, a replayed message; retry semantics are
  owned by `cratis-screenplay-streams-and-consistency`.
- Decide which promise the business needs (once-only facts are business promises, such as
  "a reservation is confirmed once", and stay specified):
  - **once-only fact**: the second attempt must not create a second fact (`unique event`);
  - **idempotent handling**: the second attempt must *succeed* without a new fact - not
    expressible as a constraint outcome; record it as a target requirement;
  - **exactly-once external effect**: an email, payment or shipment - never proven by the model.
- Shape (once-only): `when <Command>` with the earlier fact as `given` (same `for`) -> the
  constraint's fixed message. Use `when append <Event>` for projection or automation
  triggers, and to isolate a constraint the command's own validation would reject first
  (`AppendingARepeatedDeparture`); not otherwise for duplicates. See `RejectingARetriedReservation` (constraint message through `when <Command>`),
  `RejectingARepeatedDeparture` (validation first) and `AppendingARepeatedDeparture` (isolated constraint).
- n/a only when repeating is harmless and wanted (a note added twice is two notes).
- Trap: claiming retry safety from a todo-list read model plus reaction; it shows intent, not
  delivery guarantees.

## 6. Competing claim (concurrency)
- Ask: "What if two people try at the same moment? Who wins, and what does the other see?"
- Applies: any scarce or unique thing (a berth-night, a username, a seat).
- Shape: a spec cannot run two commands in parallel. Pin the loser's outcome instead: `when
  <Command>` with the *other* event source's fact as `given` (different `for`, same constrained
  value) -> `then error "Constraint '<Name>' is violated: another event source already holds
  the constrained value."` See `RejectingASecondBoatForTheSameBerthNight`. This does not prove
  the runtime's behaviour under real concurrency (`chronicle-verification.md`).
- If no constraint can hold the rule (it spans stored state), the race is unprotected: mark
  `recorded`, name the target enforcement (DCB or `[ProtectedDecision]`;
  `cratis-screenplay-streams-and-consistency`).

## 7. Alternative path
- Ask: "Which inputs change the result, and what happens either side of the boundary?"
- Applies: every `produces when` and every domain-described alternative outcome.
- Shape: for each condition a feasible true and a false witness (threshold equality where it
  distinguishes the rule). Conditions are independent: include meaningful overlaps and assert
  the full event set, every unconditional production plus each conditional one that fires. When
  all conditions are false the unconditional productions still occur. See
  `RecordingACleanDeparture` (only `BoatDeparted`) and `RecordingADepartureWithDamage` (both).
- Zero new facts: a command whose productions are all conditional may be accepted with no
  facts. Use `then no events` (ESM v17) to assert the action and its cascade added
  nothing; after `when append` it asserts nothing followed the input. Keep a
  view/query assertion too when visible state matters. Omission is not an explicit
  no-event assertion, and an action with no outcome does not bind. Do not combine
  it with events, event-order expectations, an error or denial (PLAY0545).

Requires Screenplay 4.157.0 or later (`screenplay --version`); older compilers refuse no-event assertions with PLAY0268.

## 8. Ordering and timing
- Ask: "Can these facts arrive late, early or out of order? What happens at the boundary of a
  deadline?"
- Within one event source order is the append order. Across sources (another context, a
  capture, a reaction) the view or translation must cope with any order.
- Shape: view family whose givens are in a different order; translation specs where the
  external record arrives before our own fact; `given clock`/`when clock` for deadlines
  (binds on Screenplay 4.61 or later, so not on a cratis before 3.28.2 bundle: `cratis-screenplay-toolchain` `SKILL.md`; see
  `cratis-screenplay-automations-and-translations`).
- n/a: a single event source, with no external or cross-source input.

## 9. External failure
- Ask: "What if the other party refuses, never answers, or answers twice?"
- Applies: captures, translations, reactions whose realization calls out.
- Shape: the reported outcome is a **fact** (a refusal received is an event), specified in the
  Translate or Automation slice; a missing answer is a time-based scenario. A rejection
  (`then error`) is only for requests we refuse ourselves.
- **Shown:** a failure fact that someone must act on reaches a view the person reads (a status
  literal and the reason, mapped from the failure event). Pin it with a view specification:
  `given <OpeningFact>`, `given <FailureFact>`, then `then query` with the failed status and
  the reason. A failure only an automation sees is covered by the review question in
  `cratis-screenplay-automations-and-translations`. A failure a person must act on that no
  screen shows is a gap.
- **Recoverable:** after the failure fact, the retry the business allows is accepted:
  `given <FailureFact>`, `when <RetryCommand>`, `then <AttemptFact>`. It catches a once-only
  constraint (`unique event`) that locks legitimate recovery out, and is the opposite case to
  section 5 (duplicate action). `RetryingPaymentAfterADecline` in `invoicing-dues-example.md` is the
  complete form. When the business forbids a retry, the case is a state violation with its message.
- n/a: no external system in the path.

## 10. Compensation
- Ask: "Can this be undone or corrected? What must undoing release or restore?"
- Shape: the reversal command's own specs, plus a spec proving the effect of the reversal:
  a released claim (`ReservingABerthNightFreedByACancellation`) or a removed row
  (`BerthBoardLifecycle3Cancelled`). Corrections are new facts, never edits of old ones.
- When undoing one fact obliges follow-up work (a refund after a paid booking is cancelled), the
  follow-up is an Automation slice. Specify it there: `given <PriorFact>`, `when append
  <CancellationFact>`, `then <RefundRequested>` (or the pending-work row), and the case where no
  compensation is owed (cancelled before payment: no refund fact). Recipes:
  `cratis-screenplay-automations-and-translations` `references/cases-to-specify.md`;
  `RequestingARefundAfterAReversal` in `invoicing-dues-design.md` is the complete form.
- Often reveals a missing event: if nothing can undo a fact the business can reverse, ask.

## 11. Evolution compatibility
- Ask (when an event's contract changes): "Do facts already stored still mean the same?"
- Compatible change (added property with a defined default): declare the new generation;
  specify behaviour on the new generation; write the default and its reason for old facts in `documentation`
  (Screenplay does not express upcasters). Change of meaning: a new event, with its own specs.
- Renaming a stored event keeps an `id "<OldName>"` pin. A rename the brief requests is done in
  this pass by the identity owner (`cratis-screenplay-modeling-lifecycle`); never leave it silently unapplied.
- Event evolution and generations: `cratis-screenplay-streams-and-consistency`.
- n/a: new contracts never stored anywhere yet.

## 12. Views
See `view-and-story-specs.md`.

## Example data
Read the local context first: the target declaration, its concepts, rules, gates and the
producer/projection/query mappings, and the specs already there. Follow the value path (command
input -> event payload -> projected field -> query result); matching field names alone do not
mean values should agree. Do not load the whole model to fill one fixture.

Preserve existing intentional examples. Add missing cases instead of rewriting a happy-path
fixture; correct a value only to fix a demonstrated contradiction, and say so. Never change
rules, gates, mappings or contracts to make an example pass.

- One small synthetic cast per feature with deterministic ids, so one reservation reads as a
  story across reserve, depart and cancel; each spec still states its full world.
- Values follow declared types and concept rules: UUID literals for UUID concepts, typed numbers
  and booleans, quoted declared enum members. Dates are concrete ISO dates in the story's
  future, instants offset-qualified; fix `given clock` when asserting occurrence-time values.
  No `{uuid}`/`{now}` placeholders. No real personal data.
- Structured values: single-line JSON with quoted keys and required members; empty, single and
  several items where that changes behaviour.
- Competing claim: different source ids, same constrained value. Retry: same source id and inputs.
  Removal: an existing target, plus a second row that must survive. Branch: inputs that flip the
  condition. Denial: valid business input, vary only the caller. Validation: satisfy every other
  rule.
- Unknown domain values stay questions, not plausible inventions.
- Show the deciding values in the spec body: the values the rule reads (the command card's
  `Reads:` line in slice-design `references/command-inventory.md`), the identities tying the
  steps together (the same `for` and identifier throughout) and the asserted outcome. Choose them
  at the edge that matters (one day past the limit, the amount exactly at the cap), never vague.
  Screenplay requires every required property on givens, commands and expected events, so
  put the non-deciding values in a typed `example` fixture at feature scope and write only
  the deciding values as step overrides; reuse an example only where its meaning agrees across
  features. If a rule needs a value no declaration carries, add the property (an input, event
  field or view field) instead of explaining it in `description`.
- Comparison differs by `then` form. An event `then` states every required property and the
  whole set of new facts must match (assert nothing extra, in order unless `events in any
  order`). `then readmodel` and `then query` match as a subset of the stated properties unless
  `exactly` is written, and a query's row count is always exact. Use `exactly` only when an
  unexpected extra property must fail.
- A spec `description` names the behavior witnessed or an outside condition or regulation.
  It never carries a fact that belongs in a `given`, a value or an outcome.
- Handoff after fixture work reports: the spec names added or changed; each meaningful fixture
  change and why (a demonstrated contradiction, never a rule changed to fit); which existing
  intentional examples were preserved; where a reused value came from (the event, command input
  or mapping it is traced to) and where values could not be traced; and unresolved
  inconsistencies between specs. Check the resulting source, not just that a write succeeded; a
  clean compile does not show the expected outcomes are right.

## Redelivery selectors

For recovery intent, `when redelivered <Event> to <Reaction>` must select one
definitely matching effective given and no undecided candidate. Name `stream`
(with a complete id) or `no stream` when givens differ only by route; omitting it
is a wildcard, not an unrouted selector. Redelivery reference-executes as ESM v17:
it fires only the named reaction, never re-appends the input, then settles new
facts. Use it for retry/idempotent reaction/recovery cases, not command resubmission,
clock repetition, replay exclusion or proof of external exactly-once effects.
Details: `cratis-screenplay-specifications` `references/examples-and-routes.md`.

## Scenario types at a glance

| Type | Question | Usual Screenplay shape |
|---|---|---|
| Happy path | What facts does a valid request establish? | `when Cmd` -> every firing production, payload exact |
| Rule rejection | Which input is refused, with what message? | one spec per rule; `then error "<exact message>"` (prefer `$strings.<key>`); always pinned (a bare `then error` is valid language but not accepted here) |
| Denial | Who must not do this? | `given caller` without the policy -> `then denied`; one per gated command and query, only where the whole Boolean gate is false; ownership claims need other-owner denial |
| State violation | Which prior facts make this wrong? | givens + constraint (`unique event` exclusivity) -> `then error`; else `recorded` |
| Duplicate / retry | Done again to something in that state, or sent twice (lost acknowledgement)? | `when <Command>`, same identifier, earlier fact as `given` -> constraint message, or the accepted repeat |
| Competing claim | Two people claim the same thing at once? | `when <Command>`, other source's fact as `given` (different `for`) -> constraint "another event source..." |
| Alternative path | Which branches exist, including "nothing happens"? | true and false witnesses per `produces when`; unconditional productions always fire |
| Ordering | Facts late or out of order? | view family with reordered givens; translation specs; else `recorded` |
| External failure | The other system refuses, times out or answers twice? | outcome as a recorded fact (capture/translation), never a fake rejection |
| Compensation | How is this undone, what does undoing free? | reversal command + spec showing the released claim or removed row |
| Evolution | Do old facts still mean the same after a contract change? | spec on the new generation; compatibility reasoning in `documentation` |
| View | What does each screen show after each fact? | `given` events (`for`) -> `then query`; `when append` -> update/removal |
