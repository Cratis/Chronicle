<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/consistency-and-concurrency.md -->
# Consistency and concurrency

## Contents

- 1. Invariant table (fill before writing constraints)
- 2. Where a rule can be enforced
- 3. Record each state-dependent rule that the model cannot enforce
- 4. Concurrency scenarios
- 5. Three different guarantees (do not conflate)
- 6. Lost acknowledgements
- 7. Ordering
- 8. Projection lag and rebuild
- 9. Corrections and backdated facts
- 10. Specification limits (no local spec engine)
- First append into a scope

For installed tool versions see `SKILL.md` of `cratis-screenplay-toolchain`.

Routing identity checks verified: PLAY0515 refuses personal/secret
concepts in stream-id types, composite parts and route sources, including nested
paths. Composite keys require every named scalar part, not a legacy separator
split. Text must be well-formed, nonempty NFC and Double integer ids bounded by
±(2^53−1); PLAY0504 is an error for routed nominal destination-type mismatch.
Canonical specification comparisons preserve part order and treat UUID case
variants as equal. Key-schema evolution for stored events requires a new stream
identity or migration. These checks do not protect command decision reads.
Details: toolchain `references/sources-and-streams.md`.

## 1. Invariant table (fill before writing constraints)
| # | Rule (business words) | Data it needs | Decided atomically where | Screenplay construct | Status | Race / retry outcome |
|---|---|---|---|---|---|---|
| 1 | an email belongs to one member | email on `MemberRegistered` | append-time constraint, whole sequence | `constraint UniqueMemberEmail` | enforced | second registrant rejected with the fixed message |
| 2 | one active enrolment per member and course | courseId + memberId | append-time constraint | composite `unique … released by` | enforced | racing enrolments: one rejected |
| 3 | an enrolment is recorded once | enrolment stream history | append-time constraint | `unique event MemberEnrolled` | enforced | append rejected either way; the target tells a true retry (same operation key, same payload: return the original outcome) from a conflicting request on the same stream (different payload: reject) |
| 4 | enrolments never exceed capacity | count over enrolment streams + course capacity | **not in the model** | `reads CourseAvailability` + `require … message "The course is full"` as stated intent; slice `description` + `STATE.md` | recorded (NOT enforced) | concurrent enrolments can overbook; the enrolment-to-course fan-in cannot be guarded, so the target decides a redesign (§3 options a/b) |

Status describes how the model treats the requirement:
- `enforced`: a supported construct the model declares covers the rule and every write path
  that can break it;
- `recorded`: a true requirement the model cannot enforce (NOT enforced in the model today);
  the target must, and the row names the target construct;
- `eventual by choice`: the business accepts temporary disagreement; name detection, the
  compensating fact and the consequence that remains possible.

`enforced` is a declaration, not a runtime test result. In Chronicle the kernel checks a
constraint at append, but before Chronicle 19.59.6 (InMemory: 19.59.3) the SQL and InMemory providers do not settle unique claims,
and index updates run after commit; composite values can collide when a component contains the separator; name the target's storage
provider when you rely on a constraint, record runtime evidence separately, and never claim
atomic index-and-append durability. Per row, cite the exact construct or description address
and the scenario names; on extracted models keep the legacy evidence locator and separate an
observed legacy check from verified atomic protection.

## 2. Where a rule can be enforced
| Rule depends on | Place | Notes |
|---|---|---|
| one command property | concept `validate` (format) or command `validate` | every failed rule rejects, whatever its severity |
| several command properties / constants | `require` | runs after property rules, before production |
| a value across all sources | `unique <prop> on <Event>` | value owned by one source; same source may re-claim |
| a fact at most once per source; mutually exclusive facts | `unique event` (+ `released by`) | covers simple lifecycle transitions |
| state-dependent rule (balance, capacity, "only if approved") | eligible `reads` + `require` under `command reads protected` (v25) | required absence rejects; matching intervening facts conflict. Payload-key fan-in, joins, reducers and other refused shapes remain target gaps |
| a view (read model) | unguarded materialized reads are not protection | legacy command and trigger reads remain unprotected; protected-read rendering needs Stage 4.138.0 (STAGE-ESM-044 otherwise); semantic Host/executor evidence is not generated-app protection |

## 3. Record each state-dependent rule that the model cannot enforce
Never accept the caller's copy of the state: `currentStatus Status` on the command with
`require currentStatus == "approved"` protects nothing, and neither does a boolean attestation
input (`confirmsX == true`) standing in for the rule. Never move the rule into `handler` or
`implementation hint` prose; a description renders only as an XML doc comment (Stage#178), so prose alone leaves
the rule unenforced in rendered scope.

First inspect [protected reads](../../cratis-screenplay-command-surface/references/protected-reads.md):
opt eligible command reads into v25, retain absence/conflict specifications and verify
target consistency separately. Stage admits v25 and renders protected reads from 4.138.0 (STAGE-ESM-044 otherwise).
Semantic Host/executor support has separate severity and outside-append limits. A payload-keyed
fan-in such as the course-capacity example remains refused; the preamble is not a
blanket admission of arbitrary projections.

For legacy models without opt-in or refused shapes, write stated intent and mark it unenforced.
- `reads <View> as x by …` plus `require <expr over x> message "…"` on the command (the
  `StateChange` slice in `streams-example.md`). Binding reports PLAY0271 / PLAY0268; that is
  expected in design mode, so keep the lines. Do not remove them to pass a tool.
- Slice `description`: one short marker per rule, `NOT enforced in the model today; target: <mechanism>`.
- Slice fenced `documentation`, one block per rule:
  - Requirement: the business rule in business words.
  - Status: NOT enforced in the model today (Screenplay#129).
  - Decision inputs: the authoritative facts or state.
  - Missing guarantee: why the construct does not protect the decision.
  - Race consequence: what two individually plausible commands could jointly violate.
  - Intended enforcement: the target mechanism ("Target realization" below), or the options awaiting agreement.
  - Business outcome: what a rejected, delayed or compensated request means.
- A `STATE.md` row with owner and revisit condition. A general note "some state rules are
  unsupported" does not replace these per-rule blocks.
- A specification written as a business question when the model cannot run it; do not count
  it as passing.

Options to discuss with the domain expert (decide, do not assume):
a. **one stream is the boundary**: put the decision on the stream that owns the state (often a
   different identity choice) and enforce it in the target with a concurrency scope, plus the
   first-append check described under "First append into a scope" in SKILL.md (a scope on an empty
   source or partition is not checked by default);
b. **reservation by identity**: if the scarce things can be named (seat 1..N, slot at 09:00),
   give each its own identity and use uniqueness, after checking it expresses the invariant;
c. **accept and compensate**: allow the over-commit, detect it in a view or automation and
   record a compensating fact (`EnrolmentWaitlisted`); only when the business agrees.

Target realization (for the handoff, not the model): in an Arc/Chronicle target (Arc 22.39.0 or
later, Chronicle 19.23 or later; see the Requires line below) the rule is a `[ProtectedDecision]` command reading `DecisionRead<T>` (or `IDecisionReads.Get<T>(key)` for
another event source) in `Provide()`/`Handle()` and returning a validation result; a conflict
at commit is reported as a concurrency violation. Equivalently a Chronicle decision-consistency
read with a `concurrency` scope on the event source or stream. A passive read is fresh
computation, not an atomic read-and-append; a plain materialized view and a validator are
advisory under Arc read protection `Off` (the default); under `On` (Arc 22.106.0 or later) the read
model parameters of `Handle()` and of a validator constructor are guarded. Protection covers a single
projection keyed directly by the event source id (routing key empty or `$eventSourceId`) and an
unfiltered reducer without constructor dependencies. Filtered reducers, reducers with dependencies,
joins, children, nested sources and **event-property routing** (fan-in from many streams by a payload reference) are refused, and a concurrency scope
on the referenced source does not see appends made on other sources. A rule over such a view, for
example a course capacity computed from enrolment streams, needs option a or b: write the
deciding fact on the source that owns the state, or give each scarce item its own identity. Sources: Arc `Documentation/backend/csharp/chronicle/read-models/injecting-into-commands.md` (limits unchanged);
`chronicle-boundaries.md`.

**Availability in Stage-rendered applications.** The Stage scaffold pins Arc and Chronicle client/kernel
releases (not the latest). `[ProtectedDecision]` and `DecisionRead<T>` /
`IDecisionReads` exist in that package set; source checks are in
`cratis-stage-rendering-and-sandbox` `references/rendered-application.md`.
This does not mean Stage emits protected decisions. Gap-fill must opt into the
protected command profile, use an admitted projection/key shape and enroll the
read in the active unit of work. For refused shapes choose option a/b (state on
its owning stream or one identity per scarce item), or agreed compensation c.
Keep the rule **NOT enforced in the model today** with its race consequence.
Never infer protection from `.play` `reads` + `require`, weaken the rule to render,
or edit managed dependency files. A rendered scaffold pins its own Arc and Chronicle releases and does not move with newer ones.

Requires Arc 22.39.0 or later and Chronicle 19.23.0 or later for `[ProtectedDecision]`.

## 4. Concurrency scenarios
For each enforced or recorded rule write three short scenarios:
- **race**: two commands for the same claim at the same moment -> which one wins, what the
  loser sees. With constraints: one append commits, the other is rejected. Note: Chronicle
  checks uniqueness before the commit and updates the index after it; index update failures
  are logged, not rolled back (Screenplay `constraints.md`, Chronicle#4123, open). Do not call it an
  atomic index-and-append guarantee.
- **retry** (same request and operation identity, after a lost reply) -> must not record a
  second occurrence, without blocking a different legitimate operation. A once-only fact uses
  `unique event`; a repeatable operation needs an operation identity and target-side
  idempotency. Say which guarantee the model actually supplies.
- **duplicate intent** (same business intent, new identifier) -> caught only by a
  property constraint over the business key, or not at all; decide which.
Chronicle applies optimistic concurrency to appends by default; the `concurrency` block
narrows the scope (eventSource, sourceType, streamType, streamId, events). It does not bind
in the executable model; it documents intent for the target.

See "First append into a scope" in SKILL.md.

## 5. Three different guarantees (do not conflate)
| Guarantee | Meaning | Screenplay evidence |
|---|---|---|
| once-only event occurrence | the fact exists at most once per source (or value) | `unique event` / `unique … on` + violation spec |
| idempotent command handling | repeating a request yields the same outcome to the caller | target behaviour; the model shows a rejection, the target maps "already has the event" to success where intended |
| exactly-once external effect | the outside world sees the effect once (email, payment) | cannot be shown by the model; see `cratis-screenplay-automations-and-translations` |

## 6. Lost acknowledgements
The append may commit while the reply is lost. The caller retries. Decide per command:
- **same request**: same stable operation key (or stream identifier) and same payload ->
  `unique event` rejects the append; the target compares the payload and returns the
  original outcome to the caller (idempotent handling);
- **conflicting request on the same stream**: same key, different payload -> a real
  rejection the caller must see; never report it as success;
- **new identifier** -> duplicate unless a business-key constraint exists.
The model shows only the rejection; payload comparison and outcome replay are target
behaviour. Write the answer in the slice `documentation`.

## 7. Ordering
- Within one source, facts are in append order. Across sources, do not assume an order
  unless a single sequence guarantees it in the target; views combining sources must tolerate
  any arrival order (a child before its parent). Pin that order with `given <ChildEvent>`, then `given <ParentEvent>`,
  then `then readmodel`; also cover a given child followed by `when append <ParentEvent>`.
  The reference evaluator defers the child until its original parent exists. A parent
  that never arrives leaves the child absent; an ambiguous parent still fails closed.
  This is reference execution, not a Chronicle runtime/race proof. Complete example:
  [child ordering](../../cratis-screenplay-specifications/references/child-ordering.md).
  Requires Screenplay 4.149.1 or later (`screenplay --version`) for child-before-parent ordering.
  Sources: Cratis/Screenplay
  `Documentation/screenplay/projections/semantic-model.md` and
  `Source/DotNET/Screenplay/Semantics/Execution/SemanticScopedProjection.Deferred.cs`.
- Constraints are evaluated in append order, including earlier appends of the same command.

## 8. Projection lag and rebuild
- Views are built after the append; a screen may show the previous state briefly. An
  unguarded materialized view is never a protected decision input. A passive read is fresh
  computation, and only a guarded decision read on an admitted projection shape (section 3)
  protects the interval between reading and appending.
- A rebuild replays every fact, old generations included: projections must handle every
  shape still stored, use only event data and `$eventContext` values (never "now"), and keep
  side effects out of projections.
- A todo list rebuilt from history must not re-trigger work that already finished (its
  closing facts are history too).

## 9. Corrections and backdated facts
- Facts are immutable in the model. A wrong business fact is corrected by a new fact the
  business names (`ResultCorrected`, `PaymentReversed`), and views apply it. Chronicle
  redaction and crypto-erasure exist for compliance only (prohibited content, erasure
  obligations): separately authorized operational work, not domain undo and not a scenario step.
- `$context.occurred` is when it was recorded. If the business has an effective date that can
  differ (backdated sickness, late-reported delivery), model it as a property and decide which
  date views and rules use. Constraints and reactions still see record order.

## 10. Specification limits (no local spec engine)
- A spec can show a source re-claiming its own value, a `unique event` violation and a collision
  between two different sources: `for` on `given` events (ESM v2) establishes a claim on one source
  and attempts the same value on another (`specifications.md`; `streams-example.md` uses it). It is a
  sequential history, not a race proof. The `constraints.md` sentence that it "cannot yet show a
  collision between two event sources" is stale (Screenplay `constraints.md` versus `specifications.md`).
  Spec outcomes come only from a reference run (V4) or rendered tests. See `cratis-screenplay-scenario-coverage`.
- Constraint violation messages are fixed strings: copy them exactly into `then error`.
- For every value constraint write a competing-claim spec (one source owns the value, a
  different source attempts it) using `when <Command>` with the earlier fact as `given`.
  Repeating the same source tests another rule (re-claim). When release is declared, also
  cover release followed by a claim from another source.
- These are sequential histories. They do not run simultaneous commands or prove race safety;
  concurrent execution and failure-recovery tests belong to the target implementer, reported
  separately.

## First append into a scope

**First append into a scope.** The default optimistic
strategy compares the scope's tail sequence number. When nothing matches the scope yet (the
first event on a new event source, or the first event of a new source type, stream type or
stream id), there is no tail and the append is **not checked**: two writers opening the same
source or narrowed partition can both succeed. This is the append most exposed to a race, so a
concurrency scope alone does not protect "only one writer may open this". For a target that
needs it:
- enable the check, application-wide with `ConcurrencyOptions.CheckFirstAppendIntoAScope`
  (default `false`) or per append with `ConcurrencyScopeBuilder.ExpectingNoMatchingEvent()`,
  and handle the `ConcurrencyViolation` it can now raise; or
- use a unique event constraint, which the kernel enforces for every writer whether or not the
  append declared a scope (the model's `unique event`).
Require target evidence that the check ran: a concurrent first-append scenario against a real
Chronicle, asserting one append wins, one is rejected, and `IAppendResult.ConcurrencyCheckPerformed`
is `true` (a skipped check and a passing one look identical otherwise; a current client against
an older kernel always reads `false`). Treat the default as unchecked until target evidence shows otherwise (a future major release can flip the default, so read the target's release notes). The Stage scaffold pins Chronicle client/kernel and Arc releases (see `cratis-stage-rendering-and-sandbox` `references/rendered-application.md`;
the CLI bundle's Chronicle follows the installed CLI: `cratis --version`); the APIs exist for gap-fill, not as automatically rendered enforcement.
