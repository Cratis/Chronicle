<!-- cratis-ai-managed: skills/cratis-screenplay-reverse-engineering/references/synthesis-rules.md -->
# Synthesis rules

## Contents

- Signal to element
- Event model readout
- Intent in the mapping
- Naming
- Writing through MCP
- Specifications
- Slices and flows
- Finding crosswalk
- Stop conditions

How black-box signals become candidate Screenplay elements. Signals are hypotheses: each result
is a proposal with evidence ids, decided later by a person. The translation rules for code
signals (tests, generic engines, reference data, ordering) are in
`../../cratis-screenplay-legacy-extraction/references/signal-to-intent.md`; do not repeat them.

## Signal to element

| Signal | Candidate | Notes |
|---|---|---|
| Observed form submit, or a mutating request (`POST`, `PUT`, `PATCH`, `DELETE`) | a command and a past-tense event | name the business decision, not the verb; one command per decision a person makes |
| Several requests from one user action | one command; the others are reads or consequences | keep the order seen in the capture |
| What a screen lists or shows | a read model and a query | built for the screen, not mirrored from tables; each displayed value traces to a command input, a query result or a derivation, else `unknown` |
| Filters, sorting, search boxes | query parameters on that query | |
| Before and after state pairs | specifications | given the state before, when the command, then the event and the view after |
| Rejection or validation message | a refusal rule on the command, with the exact text kept | one specification per observed refusal |
| Denied or hidden for a role | `authorize` plus a denied specification | only when two roles were observed; otherwise `unknown` |
| Navigation and journeys | slices and flows | a journey groups slices; a page is not a slice boundary |
| An email, file or call to another system seen after a command | a reaction or translation | only with an evidenced trigger; see `cratis-screenplay-automations-and-translations` |
| Server-sent state change without a user action | an automation candidate | usually `unknown` until the trigger is evidenced |

## Event model readout

Before the first `.play` write, write `event-model.md` in the work area so the human can
review the behavior in prose. It holds: the actors; a timeline per stream; each command with its
rules and evidence citations; each view and what feeds it; automations and cascades; state
machines as a table (status, allowed commands, rejected with) and a diagram; and open questions.
Mark each rule `sourced` or `assumed` and keep a count of both, so a sparse source shows up as
such. Ask which rules the dialect can express before the prose settles; a rule it cannot express
is recorded here as a gap, not dropped. Keep the file current when the model changes, and list
every rule or automation left out of the run-only copy with its reason (the complete model stays
the deliverable; see "Runnability" in [fidelity-check.md](fidelity-check.md)).

## Intent in the mapping

- **Caller identity is not an input.** Model the owner or creator as the signed-in caller
  (authentication, a policy that matches the claim to the subject, `given caller` in
  specifications), never an `actor` or `userId` property the user types. Run with sandbox users
  so the walk can act as each one. Role overrides (host, admin) you cannot express are recorded
  as a gap.
- **Ask "what would the user have to type?"** Commands a person runs ask only what that person
  knows. Values the system already knows (length, status, notice) come from a protected read in
  the mapping. Derived or internal values (minute of day, minutes until start, day keys) are
  derived, or recorded as a language gap, never exposed as a field. A value computed from text
  (tags parsed from content) is a caller input marked `assumed`.
- **Data decides, not the user.** Behavior chosen by data (needs confirmation, already paid) is
  one command whose branch lives in a derived property of the read model, not a pair of buttons
  the user picks between. A `produces when` cannot test read state.
- **Every constraint and guard gets a user-facing `message`.** Without one the UI shows the
  engine text. A missing instance behind a `reads` guard is rejected with a generated message;
  specifications assert that text, not the custom one.
- **Lists show display values**: the full name or title, not an id, a first name or split
  columns. Pickers and tables use a joined display value; field labels come from
  `field <name> label "..."`, and the walk checks that the label shows on Stage. Where the
  dialect cannot compute a display value (no concatenation; handler bodies are refused by
  Stage), store the parts or the display value on the event and record the gap in
  `event-model.md`; do not rely on a handler body.
- **A `reads` guard needs a small join-free state read model** next to the display view (one per
  guarded entity, no children, a removal event). A view with joins cannot be the guard.
- **Public feeds do not list private records.** Express visibility in the read model (a filter,
  a per-owner view); a list without a filter returns everything the caller can see.
- **Generated identifiers** are not returned to the HTTP caller unless the command declares a
  `returns` clause; otherwise clients find the new row by re-querying a view.
- **Time facts**: state the rule with its time fact and say in `event-model.md` whether the
  caller or the system supplies it. Without a clock the caller passes the time fact in; with one,
  use it. Spec the boundary both exactly equal and one below. Date and DateTime values compare
  in command rules on a Screenplay release with Date/DateTime comparisons
  (`cratis-screenplay-command-surface`). Calendar and clock forms (`today - 7 days`,
  `date of occurred`, `time zone`) bind from ESM v37 (Screenplay 5.5.0), but only in command
  conditions; see command-surface `references/date-comparisons.md`. A today-relative query
  (overdue, due today, dormant) cannot be a declarative filter: query filters admit only
  caller-supplied equality, and a clock operand there is PLAY0268.
  Do not leave such a rule as a comment. A comment expresses nothing, and Stage cannot serve it.
  Give the query a `performer` body (`cratis-screenplay-read-surface`, "Performer bodies are
  rendered") that applies the probed threshold, the inclusive or exclusive edge and the excluded
  states, and name the threshold as a constant in the body. Stage renders the body, so the
  rendered app serves the rule. The semantic sandbox, `screenplay test` and Stage
  spec runs leave a performer body out, so verify a performer-held
  rule against the rendered app (`cratis run --engine eventmodel`, or render) or fixture-dated
  specs; mark it `performer-held, sandbox-unverified`, never `expressed-and-checked`. Specify the boundary with `given readmodel` rows on fixed dates
  (threshold minus one, equal, plus one), and record "today-relative filter not declarative"
  as a platform gap row in `event-model.md` (search the Screenplay and Stage trackers first and
  comment on a matching open issue rather than filing a duplicate).
  An unprobed threshold stays `unknown`; never write a guessed number.
- **Queries**: an optional argument seen in UI traffic, a capture or a probe becomes
  `filter <name> <Type> optional` on the query; "observed to filter, model does not declare"
  is not an acceptable note.
- **Toggles clear what the inverse clears**: when a probe shows the inverse command reset a
  field (a completed-at timestamp on reopen), the projection resets it too.
- **Ownership and state-reading authorization** ("only the owner may change X"): probe it as
  a second caller, record it as an authorization finding, and never drop it. Model it as a
  state-reading policy: `reads <Model> as <alias> by <property>` plus a state condition in
  `require` (`article.author == $identity.id`), as in command-surface
  [policies that decide from recorded state](../../cratis-screenplay-command-surface/references/policy-persona-seed.md#policies-that-decide-from-recorded-state).
  Admitted shapes are limited (the caller's identifier is `$identity.id`, compared with `==` or `!=`;
  constants only); a rule outside them is a `GAP` line citing the matching open Cratis/Screenplay issue
  (search the tracker) with the rule kept as an anchored `question` note. Do not invent syntax.
- **Cross-field rules** ("a first or a last name"): write a declarative
  `require <condition>` with a `message`, for example
  `require firstName != "" or lastName != ""`. A fenced code body is a last resort; its
  shape (yielded messages, the fields it can read) is in `cratis-screenplay-command-surface`,
  and a body Stage cannot compile makes that command unsupported.
- **Cascades and observed defects**: a cascade (deleting a parent removes its children) is an
  automation: model it when the dialect allows, else list it in `event-model.md`. Model the
  intended behavior of an observed defect and flag the defect.

## Naming

- Events are past-tense business facts. A CRUD mirror (`CustomerUpdated`, `OrderSaved`,
  `RecordChanged`) hides intent. Split by intent: what did the person decide? When the
  evidence shows only that fields changed, propose the most likely intents as separate
  candidates, mark each `Inferred`, and ask.
- Use the visible labels and document terms as the business vocabulary; one name per concept.
- Commands name the decision (`ReserveBerth`), not the endpoint (`PostReservation`).
- Strongly typed identities and values come from the field names and shapes in the evidence
  (`cratis-screenplay-command-surface`); do not use raw strings and numbers for domain values.

## Writing through MCP

All writes use the Screenplay MCP server of `cratis-screenplay-model-authoring`: pick the
`propose-*` tool for the edit, read the proposal (`read-proposal`) for its diagnostics and
before/after, run `run-specifications` with the proposal id, then `apply` the reviewed
proposal. The contract and the refusal handling belong to that skill; this skill adds only:

- One slice per loop, so a refusal names one cause.
- A diagnostic is a finding. Fix the model or record the gap; never edit the file by hand to get
  past it, and never weaken protection to make it compile.
- Notes: record each open unknown as an anchored `question` note, and the evidence ids in the
  slice `description`.

## Specifications

- A specification needs both sides seen: the state before and the state after. One side alone is
  an `unknown`, not a specification.
- Use observed values for given and expected data; replace personal data with fixtures.
- An event field stamped from `$context.occurred` (occurredAt, completedAt, changedAt) still
  needs an explicit value in every `given` and `then` of a specification; a missing one is
  PLAY0524, an error that skips the whole folder, and Stage refuses the whole model for it.
  Write each such specification with a `given clock` line and use that same timestamp as the
  field's value in the `then`. Before validation, list every event field mapped from
  `$context.occurred` and check that each specification of that event supplies it.
- A UI success proves a state change was shown. It does not prove uniqueness, idempotency or
  atomicity: those stay `unknown` until a capture or a person confirms them.
- Optional properties are not defaulted in specifications, so each specification must list
  them; avoid `optional` on commands and events unless the model needs it, and use typed
  `example`s instead of repeating fixtures.
- A list result is written as `item` lines or `none`; a deleted row is a query with no
  `result`. A read model specification cannot `given` events of another stream, and DateTime
  values are exact round-trip strings. Check the specification vocabulary skill before guessing.
- For which scenarios a slice needs (refusals, retries, races, removal), use
  `cratis-screenplay-scenario-coverage`.

## Slices and flows

- Group evidence by the journey observed, then split at decisions: one slice per decision or per
  view. Three to twelve states per flow is typical.
- A read model is shaped by the screens that use it. Where two screens need conflicting shapes,
  make two read models.
- Order slices as the user meets them or as events flow; expect "view drawn before its
  producer" notes either way, they are not defects. A command that records a second stream's
  fact uses the `produces ... for <field>` form, not a second command.
- Where the capture shows ordering (create, then list, then edit), keep it as the flow, and
  state which ordering is observed and which is assumed.

## Finding crosswalk

Before validation, list every numbered finding and probe row of `re/PROBES.md` with the model
construct that encodes it (projection line, `require`, filter, performer, spec) or a `GAP` line
with its cause: `skill` (not yet encoded), `platform` (the dialect cannot express it) or
`not probed`. A query that projects a time rule also needs the fields it reads (received-on,
last-activity, stage status, completed flag): per rule, cite the `from <Event>` line that
populates each; a finding that an event sets a field with no projection line is a defect.
A finding with neither is unfinished; a `skill` cause is fixed now, not reported.

## Stop conditions

Stop and ask rather than synthesize when:

- an element would rest on a single `Inferred` row with no corroboration, and it carries a rule,
  authorization or money;
- two sources conflict and the difference changes behavior;
- the model would need protection (`pii`, `secret`, authorization) that the evidence does not
  show: model it as an open question, never silently drop it.
