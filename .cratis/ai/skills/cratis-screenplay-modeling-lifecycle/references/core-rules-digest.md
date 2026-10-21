<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/core-rules-digest.md -->
# Core rules digest

## Contents

- Two postures, never mixed in one pass
- Elements and names
- Causality
- Roles
- Field lineage
- Open question versus decided failure
- Offline-first lens
- Anti-patterns to reject early
- Structural shapes (candidates)
- Quick self-check

A one-page card of the element, naming and causality rules that every modeling phase
relies on. It restates what the construct and phase skills own, in one place, so a session
does not reopen five skills to check a name. When this card and a construct skill differ, the
construct skill and the compiler win. Adapted from Martin Dilger and Nebulit GmbH's
eventmodeling-core-rules (see `provenance.md`), translated to `.play` constructs.

## Two postures, never mixed in one pass
- **Modeling.** Discovery, slice design, authoring. Build the process as completely as you can.
  Flag a naming slip or an incomplete precondition and move on; do not stall the step on it.
- **Critic.** Review (`cratis-screenplay-model-review`). Apply every rule strictly and report every
  violation; do not soften a finding because the model mostly works, and do not quietly add
  missing structure on the model's behalf. Findings go in the report, not into the model.

A modeling pass is not the place for a full audit, and a critic pass never edits.

## Elements and names
| Construct | Means | Name it | Reject |
|---|---|---|---|
| `event` | A business fact that already happened; immutable; a correction is a new event | Past tense, business language: `BerthReserved`, `PaymentAuthorized` | UI or machinery: `SidebarOpened`, `ApiCalled`, `ResponseReceived` |
| `command` | Business intent from a caller; can be refused; on success produces one or more events | Imperative: `ReserveBerth`, `CancelSubscription` | Queries and UI actions: `LoadOrders`, `FetchData`, `OpenDialog` |
| `readmodel` (+ `query`) | A projection of events shaped for one question; optional for any given command | A noun phrase for the data: `BerthAvailability`, `InvoiceList` | Implementation words: `...Projector`, `...Repository` |
| `screen` | What a user sees and can act on | What a user would call it: `Dashboard`, `ReservationsPage` | `...Component`, `...View` internals |
| `reaction` / `trigger` | An automated actor reacting to an event, a clock or an application signal | What it does: `BillingScheduler`, `BerthReleaser` | Handler or service names |
| `persona` | A named role with Does / Reads / Cannot | A role in domain language | A generic "User" |

An event holds captured or decided facts. A running total, an average or a status that is only
recomputed from other events is a `readmodel`, not an event. A calculation that was actually
decided at a point in time (a quoted price, an approved amount) is a fact and belongs in the
event; say which it is.

## Causality
- Every locally produced event has a modeled producer that is reachable and motivated: whatever
  the language lets produce it, for example a command or reaction `produces`, a capture, or an
  event-target projection or reducer publishing an outbound public event. A foreign event
  declared with `from` is produced by its owning system, not locally. The examples are not a
  closed list; the construct skills and checklist F13 in `cratis-screenplay-model-review` decide.
- Every command has a reachable, authorized origin, for example a screen or form action, or a
  reaction to an event, a clock or an application trigger (not a closed list; F13 decides). A
  command may have several origins and a screen several actions; judge each origin on its own.
- A process starts at a screen or form action (a blank creation form counts), at a reaction to a
  modeled event, a clock or application trigger, or at a capture appending outside facts; never at
  an unmotivated command.
- Two state changes in a row need something that triggers the second (a screen, or a reaction).
- Connections read forward: what a later step needs from an earlier one is an event or a read
  model that already exists. When a later event must change what an earlier view shows, the
  projection of that view consumes the later event; do not draw a backward dependency.
- Slices connect through events and views. Common transitions: StateView to StateChange,
  StateChange to StateView, StateChange to Automation, Automation to StateChange, Automation to
  StateView, Translate to StateView, Translate to Automation. Judge a connection by checklist
  F13 in `cratis-screenplay-model-review`, not by this list.

## Roles
Every persona has a name in domain language, what it does, what it reads, and what it cannot do,
and each "Cannot" is backed by an `authorize` gate and a `then denied` specification, or recorded
as a gap. No command is attributed to a generic "User". A persona that never acts and never reads
anything is decorative, not modeled. See `cratis-screenplay-discovery` `references/personas-and-causes.md`.

## Field lineage
Every command input, event property and read-model field has an origin (user input, caller
context, a prior event property, a decided derivation, or an external payload) and, where it
matters, a destination. A field with no origin is a gap to resolve, not a detail to fill in later.
See `cratis-screenplay-slice-design` `references/field-lineage.md`.

## Open question versus decided failure
- **Open question.** Genuinely undecided. Record an anchored `question` note in
  committed `.screenplay/notes.json`; STATE.md/reports carry its note ID and
  assumption. Not only chat or `description` text. Resolving means answering:
  cite project decisions in `refs` and resolve by `external-decision` when they
  supply the answer, then realize it as a specification, description or decision
  record. Read `stop-or-assume.md` "Durable question notes" when doing this.
  Never put secrets or personal data in notes.
- **Decided failure.** A rejection or error whose behavior is already decided ("payment fails,
  refuse, no event"). Model it as a `specification` with `then error` or `then denied`. It is
  permanent specified behavior, not a note waiting on an answer.

A decided failure left as an open note looks unresolved when it is not. When counting
completeness, only a genuinely unanswered question is a gap; a decided failure needs its
specification.

## Offline-first lens
Model the business process as it would run without software first: who acts, what triggers each
action, what information they need before acting. Question any step that exists only because of
the system (a loading state, a cache refresh, a session check); it is not a business step and
does not belong on the timeline. The anti-patterns below are what skipping this looks like.

## Anti-patterns to reject early
- **Data-loading commands.** `LoadOrders` is a query; model a `readmodel` and a `query`.
- **UI-interaction events.** `ButtonClicked` is UI state; model it only if the business cares.
- **Technical events.** `ApiCalled` describes machinery; find the business outcome underneath.
- **Recomputed events.** A value that changes whenever its source data changes is a read-model field.
- **Generic structure.** `SubmitForm` with `content`, or a record of `fields` and `entries`, says
  nothing about the business. Model one real flow with real values: `ReportHullDamage` ->
  `HullDamageReported` with berth `A-12` and "Scratched starboard bow, about 40 cm". The exception
  is a platform that is itself the product: its own facts (`FormTemplatePublished`) are the
  business. Brownfield generic engines are read the same way
  (`cratis-screenplay-legacy-extraction`).

## Structural shapes (candidates)
Check these only when the model's wiring is visible; a count over the threshold is a prompt to
think, not a finding.
- **Screen with several commands** (the bed). Check each action is a distinct decision the user
  already made on that screen. Screenplay allows several actions on a screen; the signal is a
  single submit that quietly settles several things.
- **Command with more than two events** (left chair). One business outcome that fans out, or a
  command doing two jobs? Ask whether the events always happen together.
- **Read model fed by more than three events** (right chair). One coherent picture, or a view
  answering several questions? Ask whether the fields describe one thing.
- **Slice with many more specifications than its neighbors** (shelf). Genuinely complex, or
  quietly covering a neighbor's ground? Read what the extra specifications cover.

Use plain descriptions in anything a business stakeholder reads; the shape names are shorthand
for modelers. The full signal, consequence and fix per shape is in
`cratis-screenplay-model-review` `references/anti-patterns.md`.

## Quick self-check
- [ ] Every event is past tense and a business fact.
- [ ] The model follows one real flow with real values, not a generic structure.
- [ ] Every command is imperative intent, not a data load.
- [ ] Every read model is named for its data.
- [ ] No event carries a value that is only recomputed from other events.
- [ ] Every command has a reachable, authorized origin.
- [ ] Every process starts at a screen or form action, a reaction, a clock or application trigger, or a capture.
- [ ] No two state changes are chained without a trigger between them.
- [ ] Every persona has a Cannot line backed by a gate and a denial specification, or a recorded gap.
- [ ] Every decided failure is a specification; every open question is an anchored note with its ID in session state.

This is the fast pass; the full audit is `cratis-screenplay-model-review`
`references/checklist.md`, and the closing quality checklist is in `references/phases.md`.
