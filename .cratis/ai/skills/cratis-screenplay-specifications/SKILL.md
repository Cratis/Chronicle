---
name: cratis-screenplay-specifications
description: Pin behavior in a Cratis Screenplay `.play` model with given/when/then `specification` blocks — prior events and read-model state, the caller, the command or appended event under test, expected events, read-model state, query results, rejections and denials, plus what the reference execution actually runs. Use when writing acceptance criteria for a slice, specifying a rejection or an authorization denial, or deciding whether a rule belongs in a specification or in the type system. Do not use for C# or TypeScript test code.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/SKILL.md -->

# Screenplay specifications

Read [preferred syntax](../cratis-screenplay-toolchain/references/preferred-syntax.md) when writing or reviewing `.play`.

A `specification` is the acceptance criterion for a slice, written in the same
document as the behavior it pins. Given/when/then, in business language, with
concrete data.

## Locate the model

Look first for the project's existing `.play` files: their folder is the model.
For a new model, check `Source/` or `src/`, else use `Screenplay/` at the repository
root. Never put a model under `.cratis/` or hand-copy it between repositories.
`cratis ai install` manages `.cratis/ai/`, never model files. Keep explanatory
Markdown in project documentation; `.play` is the single flow model.

## Product sources

Specification syntax follows Screenplay `Documentation/screenplay/specifications.md`.
Tool versions: `cratis-screenplay-toolchain` `SKILL.md`. Compile commands check source, not
reference execution. Use `screenplay test` or MCP `run-specifications` for V4; Stage/target
tests are separate evidence. Propose → review → `run-specifications` with `proposalId` → apply.
PLAY0285 (an impossible `then`) unbinds the whole model and blocks every spec.
Unsupported/unbound is never a pass. Typed examples and
route assertions are in [examples and routes](references/examples-and-routes.md).

Requires Screenplay 4.145.0 or later (`screenplay --version`) for `run-specifications` with `proposalId`.

Read [source notes](references/source-notes.md) for the retained inline-fact and action-syntax evidence; run the changed examples at the standalone pin.

## State every property

A `when` command and each `then` event list **every** property, including `optional`
ones, with a concrete value; omitting one is PLAY0524. Only a read-model property may be `null` (PLAY0350).
This model fails with PLAY0524 on both `when` and `then` until `note = "none"` is added:

```screenplay excerpt
event TaskOpened
  note String optional
specification Opening
  when OpenTask
    taskId = "t1"
    title = "Write"
  then TaskOpened
    for "t1"
    title = "Write"
```

## Reference execution — what actually runs

Screenplay's reference runner executes specifications against an immutable
in-memory world: no Arc, no Chronicle, no database, no network. Every downstream
target has one normalized behavior to match.

Binding is not plan admission. Read model-authoring's
[language reference](../cratis-screenplay-model-authoring/references/language-reference.md)
when distinguishing executable declarations, refused projection shapes and opaque
code. Reached opaque code is **unsupported**, never passed; portable denial may
still execute before it. An unadmitted projection makes unsupported only the specifications that reach it
(Screenplay 4.213.2 or later); other unsupported declarative constructs block the whole plan.

Read [reactions and cascades](references/reactions-and-cascades.md) when testing
ESM v6 actions; older compilers fail reaction/capture binding with PLAY0268.
Read [child ordering](references/child-ordering.md) for deferral;
an ambiguous parent still fails closed.
Requires Screenplay 4.149.1 or later (`screenplay --version`) for child deferral; older runners do not defer.

A rejected command/append leaves the world unchanged; an accepted one commits
before comparing views. V6 has no scenario-wide transaction: later cascade
failure does not roll back earlier accepted facts. Atomic mode boundaries are below.

Unsupported is not passed. Name a target. Run CLI/MCP reference execution
or target tests; name engine, version and expected/discovered selection.

## Batches, described bodies, protected reads and text rules

- With `reaction batches atomic`, assert no facts from a failed direct-production or
  selected refusal-branch batch and unchanged views through evaluator world assertions,
  not only `then error`. `then no events` is acceptance and cannot accompany an error. Earlier
  accepted batches/invoked commands remain; legacy absence retains per-fact appends.
  Read [atomic boundaries](../cratis-screenplay-captures-and-reactions/references/atomic-batches-and-source-mappings.md).
- A described handler binds v23, but reaching its body returns Unsupported
  ImplementationBody, naming command/RequirementId and emitting no events. Denial and
  validation cases still run; an all-Unsupported run is never green. Read
  [described handlers](../cratis-screenplay-command-surface/references/described-handlers.md).
- Protected command reads opt into v25. Test present/absent state and rule refusal;
  matching decide/commit changes are Conflict / DecisionStateChanged, not validation
  refusal. Race vectors are not new specification syntax. Read
  [protected reads](../cratis-screenplay-command-surface/references/protected-reads.md).
- Literal text rules use ordinal UTF-16 comparison (v24); matches uses a portable
  linear matcher, not platform regex. Old invalid/refused patterns plan as
  UnsupportedValidation, not a successful check. Read
  [text and match rules](../cratis-screenplay-command-surface/references/text-and-match-rules.md)
  for boundary values and PLAY0699/0787. Reference success is not Stage admission.

## Chronological command date comparisons

Requires Screenplay 4.199.0 (ESM v26; `screenplay --version`); older binders refuse
these comparisons with PLAY0268. DateTime conditions compare UTC instants at tick
precision: cover equal instants with different offsets, increasing endpoints,
an inverted interval and a one-tick boundary. Every participating DateTime input
without an offset or `Z` rejects with Validation before rules, even when its
comparison is skipped; add a missing-offset rejection, not a clock fixture workaround.
`then` matching stays ordinal: expected event values keep the exact produced text,
not an equivalent offset. Read the complete [retention-window specifications](../cratis-screenplay-command-surface/references/date-comparisons.md)
for `require to >= from`, PLAY0268/0273 and Stage's v26 refusal. The missing-offset
runtime case needs an evaluator-request or target input test: putting the invalid
value in a typed `.play` fixture instead fails binding with PLAY0273; `then error`
does not exercise runtime validation.

## Descriptions, persona callers and case tables

Use one `description "<rule or behavior>"` (or a fenced `text` description) to
name what the specification witnesses. It is report-only (PLAY0270), changing
neither execution nor ESM bytes. Specifications do not accept `documentation`;
put longer assumptions and rejected alternatives on the owning slice's fenced
Markdown `documentation` block.

`given caller as <Persona>` expands to one authenticated policy witness before
binding. Named `parameter`/`case` tables share steps and read values as
`case.<parameter>`; each case runs independently as `<Specification>_<Case>`.
Read [persona callers and case tables](references/persona-callers-and-cases.md)
for synthesis refusals, selection, provenance and a complete passing model.

## The vocabulary

Read [references/vocabulary.md](references/vocabulary.md) when writing or reading a `given`, `when` or `then` line, to look up a construct such as `then events in any order`, `then no readmodel`, `generated`, `then returns`, `then error` or `then denied`, or an enumeration or `DateTime` literal.

Rules the binder enforces:

- **At most one action** — a command, `when append`, `when clock`, `when trigger`,
  `when capture`, `when redelivered` or `when query` (`PLAY0097`, `PLAY0358`). Without an action,
  assert only read state or one queried call with `then error` (ESM v18; otherwise PLAY0352).
- **`then result` and `then no result` need `when query`**, and `when query`
  needs one of them, `then denied` or a sole validation `then error` (`PLAY0465`). Each argument is a `by` or
  `filter` parameter of the query (`PLAY0468`).
- **A clock instant is ISO 8601 with an offset or `Z`** (PLAY0461); one `given clock` maximum.
- **`then` contains either events or an error — never both.** A rejection
  specification has exactly one rejection and no success outcome.
- **`then denied` stands alone**: not with events, errors or state assertions
  (`PLAY0388`). For a query, pair it with one `then query` that has `arguments`
  and no `result`.
- **Authorized commands and queries need `given caller` or `given caller as <Persona>`** (`PLAY0389`). The runner
  never invents a caller.
- **`null` only in optional read-model values.** A `null` in a command or event
  value is `PLAY0350`: an optional fact is a separate event.

V1 does not report binding-only outcome errors; read `references/vocabulary.md`
for parse-versus-bind diagnostics and check executable diagnostics too.

## How outcomes are compared

Events compare as the complete new set in authored order (exact count). Read models and query rows: only the asserted properties must match (add `exactly` for all). Rejections: `then error "<message>"` matches the message, never severity. Bare `then error` never matches
  a denial. Read [references/outcome-comparison.md](references/outcome-comparison.md) when asserting events, rows, ordering or a localized rejection key.

## Worked example

A command with an authorization gate, a validation rule and a uniqueness constraint, specified in full with inline events, optional-aware queries and read-model assertions.
Read [references/worked-example.md](references/worked-example.md) when writing or checking a complete command, view or constraint specification and you need the full compiled model to copy the shape from.

## Choosing a strategy

Read [shop fixtures and strategy choices](references/generated-fixtures.md).

## When not to

Pins/clock do not realize context or secret generation; preserve the contract.

## Generated fixtures and return expectations (ESM v7)

Current compilers bind fixtures/returns. Reached generation without every pin is
unsupported, never passed. The fixture reference covers strict v15 shapes and freshness.

## Actions beyond commands

A slice is not always set off by a command: `given clock`, `when clock`, `when trigger`, `given capture`, `when capture` and `when query` (with `then result` / `then no result`) name what does. Use `given clock`, never `given time`. From ESM v6 the reference runner executes the clock, trigger and capture actions; before ESM v6 only `when query` executed among them.
Read [references/actions-beyond-commands.md](references/actions-beyond-commands.md) when the slice is an automation, translation or query view, or you need the action table, grammar and what the reference runner executes for each.

## Reactions and cascades (ESM v6)

From ESM v6 every action sets reactions off and `then` events compare every new fact, the action's and the reactions'. A specification whose action is a command goes in the slice that declares the command. A false `PLAY0285` on a cascade under a compiler that stops before ESM v6 is tool skew, not a modeling error: never delete the `then` line to silence it.
Read [references/reactions-and-cascades.md](references/reactions-and-cascades.md) when a specification's expected events include what a reaction appends, when a cascade, clock tick or invoked command is involved, or when `cratis` reports `PLAY0285` on a model that `screenplay` accepts (it holds the "Version skew: cascades and the false `PLAY0285`" section).

## Published-event specifications (released ESM v9)

Use `when append <PrivateEvent>` then `then <PublicEvent>`, not `then readmodel`;
follow-up events are exhaustive and ordered. Read
[publication and fold cases](../cratis-screenplay-automations-and-translations/references/public-events-and-direction.md)
when specifying outbound facts or event-source captures: it owns the minimum
compiler, PLAY0700/0701, key folding and Unsupported boundaries. Composite-keyed
view fixtures state every key part (Requires Screenplay 4.204.0, ESM v27); see
`references/vocabulary.md` and read-surface `references/named-keys.md`.

## Reusable fixtures, routes and recovery intent

Use typed `example` declarations for repeated event/command/read-model values;
step overrides keep the action/outcome visible and carry authored/example/override
provenance. Event examples can carry routes, replaced as a whole by a step.
Read [examples and routes](references/examples-and-routes.md) when locating a
redelivered given (v17, PLAY0543) or asserting event routes (v8); it owns typed
`for`, `stream`/`no stream`, complete ids and undecided-candidate rules.

Operations remain authoring-only (PLAY0268). Read `references/examples-and-routes.md`
when asserting `then no events` or located recovery without re-append (v17), and
[refusal handling](../cratis-screenplay-captures-and-reactions/references/refusals-and-redelivery.md)
when recording/acknowledging invocation refusals (v19).
Requires Screenplay 4.157.0 (`screenplay --version`) for v17, 4.160.0 for v19;
older compilers report PLAY0268.

## Rejections and denials say different things

`then error "<message>"` pins the reason, bare `then error` leaves it unnamed, and `then denied` is an authorization denial decided first. Views cannot reject events.
Read `references/query-rejections.md` when specifying malformed keys/filters
(Validation `then error`, v18) versus valid absence (`then no result`).
Requires Screenplay 4.161.0 (`screenplay --version`); older runners may answer absence. Read [references/outcome-comparison.md](references/outcome-comparison.md) when choosing among them.

## Business rule or concept rule?

Write an error specification for a rule that depends on existing system state or that another business could plausibly set differently. A format, range or presence rule on one value belongs on the concept with its own `validate` block, with **one rejection specification per concept rule**, through one command that uses the concept, not one per use.
Read [references/concept-rules.md](references/concept-rules.md) when deciding whether a rule needs an error specification or a concept `validate`, or when you need the complete concept-rule example with boundary acceptances and rejections.

Text `not empty`: [whitespace rejection specs and boundaries](references/whitespace-rules.md).
Whitespace-acceptance specs fail. This applies to every ESM version.

Requires Screenplay 4.151.0 or later (`screenplay --version`) for the Unicode `not empty` correction.

Date/DateTime comparisons are the command-only exception: do not move them onto
concept rules to follow the general format-rule convention.

## Application-boundary coverage

Every slice should carry at least one specification whose action is what a real
caller does and whose `then` is observable at that same boundary — produced
events, read-model state, query results, a rejection or a denial. A specification
satisfiable only by an internal function describes a unit, not a slice acceptance
criterion.

## Quality checklist

For every specification:

- [ ] Concrete, realistic values — never "valid user" or "some amount".
- [ ] The values the outcome depends on are visible in the body, at the edge that matters.
- [ ] Tests exactly one behavior, independent of other specifications.
- [ ] Business language matching the event-model vocabulary.

For command specifications:

- [ ] Every `given` event and the `when` command state all required fields.
- [ ] `given caller` is present whenever the command is authorized, with a
      `then denied` case for a caller who must be refused.
- [ ] `then` contains **either** events **or** an error, never both.
- [ ] A collision between two event sources uses `for` on the `given` event. A `given` without
      `for` belongs to the event source the specification acts on; state `for` when the command
      appends to several event sources or an assertion depends on it (see
      [examples and routes](references/examples-and-routes.md#givens-without-an-event-source)).
- [ ] Error cases test business rules; each concept rule has one rejection
      specification through one command, not one per use.
- [ ] A localized rejection is pinned by its quoted `$strings` key.
- [ ] DateTime comparisons cover offset-less input rejection before rules, equal
      instants across offsets and tick boundaries; `then` keeps ordinal payload values.

For view specifications:

- [ ] A view a `performer` composes is specified with `given readmodel`,
      `when query` and `then result` or `then no result`; the reference runner leaves such a
      spec out (`PLAY0270` information), so it is design intent, not executed proof.
- [ ] `given readmodel` is a complete instance with its identifier.
- [ ] `then readmodel` states the identifier and the properties that matter; add
      `exactly` only when extra properties must fail the assertion.
- [ ] Queries cover out-of-rule concept arguments with `then error`; a valid
      absent key uses `then no result`. Projections still cannot reject events.

## Verify

- [ ] Validate the complete root with warnings as errors, naming tool/version.
      Standalone tool version: `cratis-screenplay-toolchain/SKILL.md`. Run specification examples through
      `screenplay test`; expected unadmitted syntax reports unbound, not passed.
- [ ] Executable diagnostics are clean too: `PLAY0350`, `PLAY0352`, `PLAY0388`,
      `PLAY0389` and `PLAY0273` are only reported at binding.
- [ ] Every slice has at least one boundary specification.
- [ ] The rejections and denials are specified, not only the happy path.
- [ ] No `given`/`when`/`then` clause names an element the model does not declare.
- [ ] An unsupported run is reported as unsupported, never as passing.

## Route near misses

- Which scenarios a slice needs and how to run a scenario workshop: `cratis-screenplay-scenario-coverage`.
- Reviewing a model's specifications and verdicts: `cratis-screenplay-model-review`.
- Automation and translation slices the clock, trigger and capture actions drive: `cratis-screenplay-automations-and-translations`.
- Which tool says what, and the versions: `cratis-screenplay-toolchain`.
- The rules being specified: `cratis-screenplay-command-surface`.
- The projections being asserted on: `cratis-screenplay-projections`.
- Naming and scoping specifications generally: `cratis-specification-by-example`.
- C# specifications for hand-written Cratis code: `cratis-specifications-csharp`.
