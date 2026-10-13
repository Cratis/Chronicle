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

## Verified product sources

Retained inline-fact update evidence: [source notes](references/source-notes.md).

The historical parser/binder/reference-runner version table is in the source notes.

Specification syntax was checked against Screenplay v4.127.0 `specifications.md`;
older probes in the table remain historical. Pins: `cratis-screenplay-toolchain`
`references/versions.md`. Compile commands check source, not reference execution.
Use `screenplay test` or MCP `run-specifications` for V4; Stage/target tests are
separate evidence. On Screenplay **4.145.0 and later**, propose → review →
`run-specifications` with `proposalId` → apply.
PLAY0285 (an impossible `then`) unbinds the whole model and blocks every spec.
Unsupported/unbound is never a pass. Typed examples and
route assertions are in [examples and routes](references/examples-and-routes.md).

Read [source notes](references/source-notes.md) for the original v4.31.0
reference-runner evidence and v4.48.0 action syntax. Historical evidence is not
current execution evidence; run the changed examples at the standalone pin.

## Reference execution — what actually runs

Screenplay's reference runner executes specifications against an immutable
in-memory world: no Arc, no Chronicle, no database, no network. Every downstream
target has one normalized behavior to match.

- It runs the capabilities the execution plan admits: declarative validation
  and `require` over command properties, conditional production, literal tags,
  declarative policies, `unique` constraints, and projections as Chronicle
  lowers them. The `cratis-screenplay-model-authoring` language reference lists
  what binds and what the plan admits.
- Binding is not admission. These projection constructs bind, but the plan
  refuses them: a projection-level `remove via join`; `all` beside removals,
  `children` or `nested`; a `join`, `children` or `remove via join` inside
  `nested`; and any `$eventContext.<path>` other than `eventSourceId`, such as
  `$eventContext.occurred`. The limits apply at every ESM version.
- A specification that needs **opaque code** — a bodied reducer, a rule with a
  body, a fenced `validate` block, a code policy, or (ESM v6) a reached reaction
  body — returns **unsupported** and
  never passes. Authorization is evaluated first, so a `then denied` case still
  runs when a portable policy decides it. Other specifications in the model run
  normally.
- Unsupported reachable declarative constructs block the whole execution plan
  rather than running partially: no specification in the model runs, including
  the ones that never touch the refused construct.
- Reactions: before ESM v6 they never run, not even after `when append`. From ESM
  v6 (standalone tool from 4.61.0) they run after every action, as in "Reactions and
  cascades" ([references/reactions-and-cascades.md](references/reactions-and-cascades.md)). Under `cratis` before 3.28.2 (Screenplay 4.60.1) `when clock`, `when trigger`
  and `when capture` failed binding with `PLAY0268`, and so did reactions and captures; they
  bind on 4.66.0 and `cratis` 3.28.2.
- A rejected individual command or append leaves the world unchanged; an accepted
  one commits once, then the read models and queries are compared. There is no
  scenario-wide transaction in v6: when a later effect of a cascade or capture
  rejects, the earlier accepted facts and their projections remain.

Unsupported is not passed. Report it as "needs a target", not as green. "Specified"
and "bound" are never "passing": run the reference CLI/MCP route or the owning
target tests, and name the engine, version and expected/discovered selection.

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

| Construct | Meaning |
| --- | --- |
| `given <EventType>` | prior state, established by replaying events before the action |
| `given readmodel <ReadModelType>` | prior read-model state, established directly — a **complete** instance including its identifier |
| `given caller` / `given caller as <Persona>` | explicit caller (`authenticated`, roles, claims; empty means anonymous), or an authenticated synthesized persona witness |
| `when <CommandType>` | run a command |
| `when append <EventType>` | append one event: constraints and projections run, the command does not; reactions run only from ESM v6, where `then` lists what followed |
| `when query <Query>` | perform a query with the argument values beneath it (v4.48.0) |
| `then result [exactly]` | one expected row of the performed query; repeat for several (v4.48.0) |
| `then no result` | the performed query returns nothing (v4.48.0) |
| `given clock "<instant>"` | the ISO 8601 instant the scenario happens at (v4.48.0) |
| `when clock "<instant>"` | the clock reaches an instant; scheduled reactions that are due run (v4.48.0) |
| `when trigger <Trigger>` | an application trigger fires with the values beneath it (v4.48.0) |
| `given capture` / `when capture <Capture>` | an earlier and the current record of a capture's source (v4.48.0) |
| `generated <name> = <value>` | beneath `when <Command>`: the fixture for a generated property other than the identifier (ESM v7); `for` beneath the `when` supplies the generated identifier |
| `then returns [<value>]` | the command's response (ESM v7): `then returns "<uuid>"` for a scalar, `then returns` with named `field = value` lines for a subset of a record |
| `then <EventType>` | an expected new event |
| `then events in any order` | compare the new events without regard to order |
| `then readmodel <ReadModelType> [exactly]` | read-model state afterwards — must state the identifier |
| `then no readmodel <ReadModelType> for <key>` | exactly that keyed instance is absent (v4.66.0); the key is concrete and typed |
| `then query <Query> [exactly]` | query results for explicit `arguments` (none for an unkeyed list); one `result` per row, each stating the identifier; none means empty |
| `file <path>` | optional, once: the repository-relative file realizing the specification |
| `then error "<message>"` | a validation or constraint rejection, for that reason; a localized rule is pinned by its quoted key, `then error "$strings.<key>"` |
| `then error` | a validation or constraint rejection, reason unnamed |
| `then denied` | an authorization denial (`Unauthorized`) |
| `for <value>` | the event source of a `given`/`then` event, or of the `when` command or appended event |
| `<property> = <value>` | a literal, `null`, or a one-line JSON-shaped object or list: `lines = [{"sku":"A-1","quantity":2}]` |

From v4.48.0 an enumeration value binds whether it is written bare
(`status = sent`), qualified (`status = InvoiceStatus.sent`) or quoted, and a
`DateTime` value binds as a plain ISO 8601 instant (`"2026-10-05T08:00:00Z"`).
Before v4.48.0 only the quoted member and the full round-trip instant
(`"2026-10-05T08:00:00.0000000Z"`) bound, although the compiler accepted all of
them - quote members when a model must bind on older versions.

Rules the binder enforces:

- **At most one action** — a command, `when append`, `when clock`, `when trigger`,
  `when capture` or `when query` (`PLAY0097`, `PLAY0358`). Without an action,
  assert only `then readmodel`, `then no readmodel` or `then query` (`PLAY0352`).
- **`then result` and `then no result` need `when query`**, and `when query`
  needs one of them or `then denied` (`PLAY0465`). Each argument is a `by` or
  `filter` parameter of the query (`PLAY0468`).
- **A clock instant is ISO 8601 with an offset or `Z`**, such as
  `"2026-10-05T08:00:00Z"` (`PLAY0461`); `given clock` appears at most once.
- **`then` contains either events or an error — never both.** A rejection
  specification has exactly one rejection and no success outcome.
- **`then denied` stands alone**: not with events, errors or state assertions
  (`PLAY0388`). For a query, pair it with one `then query` that has `arguments`
  and no `result`.
- **Authorized commands and queries need `given caller` or `given caller as <Persona>`** (`PLAY0389`). The runner
  never invents a caller.
- **`null` only in optional read-model values.** A `null` in a command or event
  value is `PLAY0350`: an optional fact is a separate event.

`PLAY0358` is a syntax error (`PLAY0388` also at parse time for extra text after `then denied`). `PLAY0350`, `PLAY0352`, `PLAY0388` (mixed with another outcome), `PLAY0389` and
the one-rejection rule (`PLAY0273`) are reported only when the model binds, so
`screenplay --warnaserror` does not show them. Check executable diagnostics
(the MCP authoring tools) as well.

## How outcomes are compared

- **Events:** the complete set of new events, in authored order, unless
  `then events in any order` is stated. The count is always exact. After
  `when append`, a model before ESM v6 expects the appended fact itself; from
  ESM v6 the appended event is the action and `then` lists only what followed it.
- **Read models and query rows:** only the asserted properties must match
  (subset); add `exactly` to require every property. Query row count is exact;
  collection rows match by read model/key regardless of order. Ordering within a
  collection-valued property is unchanged. A missing property does not match an asserted `null`.
- **Rejections:** `then error "<message>"` matches the message whatever the rule's
  validation severity; no form asserts severity. Bare `then error` never matches
  a denial. Quote a localized key: `then error "$strings.invoices.reasonRequired"`.
  Besides a String `case.<parameter>`, the grammar takes only quoted messages here, never an unquoted `$strings` token
  (unlike a rule's `message` operand). The reference runner compares the symbolic key
  and requires the rejection to carry the string-key marker; it never loads
  translated text; resolving the key is the realization's job.

## Worked example

A command with an authorization gate, a validation rule and a uniqueness constraint, specified in full with inline events, optional-aware queries and read-model assertions.
Read [references/worked-example.md](references/worked-example.md) when writing or checking a complete command, view or constraint specification and you need the full compiled model to copy the shape from.

## Generated fixtures and return expectations (ESM v7)

Current standalone and bundled compilers bind generated fixtures (`generated <name> = <value>`, `for` beneath `when`) and `then returns` as ESM v7. If execution reaches generation without every generated value supplied, the run is unsupported and never passes.
Read [references/generated-fixtures.md](references/generated-fixtures.md) when writing generated-value fixtures or scalar and record return expectations for a command.

## Actions beyond commands

A slice is not always set off by a command: `given clock`, `when clock`, `when trigger`, `given capture`, `when capture` and `when query` (with `then result` / `then no result`) name what does. Use `given clock`, never `given time`. From ESM v6 (Screenplay 4.61.0 and later, `cratis` 3.28.2 and later) the reference runner executes the clock, trigger and capture actions; before ESM v6 only `when query` executed among them.
Read [references/actions-beyond-commands.md](references/actions-beyond-commands.md) when the slice is an automation, translation or query view, or you need the action table, grammar and what the reference runner executes for each.

## Reactions and cascades (ESM v6)

From ESM v6 every action sets reactions off and `then` events compare every new fact, the action's and the reactions'. A specification whose action is a command goes in the slice that declares the command. A false `PLAY0285` on a cascade under `cratis` before 3.28.2 is tool skew, not a modeling error: never delete the `then` line to silence it.
Read [references/reactions-and-cascades.md](references/reactions-and-cascades.md) when a specification's expected events include what a reaction appends, when a cascade, clock tick or invoked command is involved, or when `cratis` reports `PLAY0285` on a model that `screenplay` accepts (it holds the "Version skew: cascades and the false `PLAY0285` (cratis before 3.28.2)" section).

## Published-event specifications (released ESM v9)

Use `when append <PrivateEvent>` then `then <PublicEvent>` for outbound publication,
not `then readmodel`. The follow-up event list is exhaustive and ordered. Read
[publication example and fold cases](../cratis-screenplay-automations-and-translations/references/public-events-and-direction.md).
Events-source captures and reached opaque event-target reducers return typed
Unsupported, not a pass. Composite view fixtures need every key part but remain
unadmitted (PLAY0268, #599); see read-surface `references/named-keys.md`.

## Reusable fixtures, routes and recovery intent

Use typed `example` declarations for repeated event/command/read-model values;
step overrides keep the action/outcome visible and carry authored/example/override
provenance. Event examples can carry routes, replaced as a whole by a step.
Redelivery locators may state `stream` or `no stream` to select one definitely
matching given occurrence with no undecided candidates (PLAY0543); redelivery
remains authoring-only. Event `stream`/`no stream` assertions bind as ESM v8; typed `for` and
stream ids are not guessed. Read [examples and routes](references/examples-and-routes.md).

`given operation ... fails`, `then operation` and `then compensated` describe
future external-work behavior. Reaction refusal branches, `when redelivered`
and `then no events` (PLAY0268, #433) are also authoring-only: compile them, but report their
PLAY0268 binding refusal instead of an executed result. Preserve the contract; a read-model or
query assertion states a zero-fact outcome.

## Rejections and denials say different things

`then error "<message>"` says **rejected, for this reason** — pin it when the
message is the point. Bare `then error` says **rejected, for a reason this
specification does not name**; the reason lives in the specification's name.
`then denied` says **this caller may not do this**, which is decided before any
validation runs. Write the bare form rather than `then error ""`.

**Views cannot reject events.** A projection never refuses an event; there are no
error cases for one. (A `when append` can still be refused by an append-time
constraint — that is the constraint speaking, not the view.)

## Business rule or concept rule?

Write an error specification for a rule that depends on existing system state or that another business could plausibly set differently. A format, range or presence rule on one value belongs on the concept with its own `validate` block, with **one rejection specification per concept rule**, through one command that uses the concept, not one per use.
Read [references/concept-rules.md](references/concept-rules.md) when deciding whether a rule needs an error specification or a concept `validate`, or when you need the complete concept-rule example with boundary acceptances and rejections.

## Application-boundary coverage

Every slice should carry at least one specification whose action is what a real
caller does and whose `then` is observable at that same boundary — produced
events, read-model state, query results, a rejection or a denial. A specification
satisfiable only by an internal function describes a unit, not a slice acceptance
criterion.

## Quality checklist

For every specification:

- [ ] Concrete, realistic values — never "valid user" or "some amount".
- [ ] Tests exactly one behavior, independent of other specifications.
- [ ] Business language matching the event-model vocabulary.

For command specifications:

- [ ] Every `given` event and the `when` command state all required fields.
- [ ] `given caller` is present whenever the command is authorized, with a
      `then denied` case for a caller who must be refused.
- [ ] `then` contains **either** events **or** an error, never both.
- [ ] A collision between two event sources uses `for` on the `given` event.
- [ ] Error cases test business rules; each concept rule has one rejection
      specification through one command, not one per use.
- [ ] A localized rejection is pinned by its quoted `$strings` key.

For view specifications:

- [ ] A view a `performer` composes is specified with `given readmodel`,
      `when query` and `then result` or `then no result`.
- [ ] `given readmodel` is a complete instance with its identifier.
- [ ] `then readmodel` states the identifier and the properties that matter; add
      `exactly` only when extra properties must fail the assertion.
- [ ] No error cases — views cannot reject.

## Verify

- [ ] Validate the complete root with warnings as errors, naming tool/version.
      Standalone pin: `cratis-screenplay-toolchain/references/versions.md`. Run specification examples through
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
