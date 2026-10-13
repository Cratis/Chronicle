<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/cheat-sheet.md -->
# Screenplay cheat-sheet (tool versions: versions.md)

## Contents

- Samples and preview fixtures
- Lexical and structural rules
- Exact numeric source mode
- Clause order inside a command (canonical)
- Slice contents (typical)
- `$context.` paths (command `produces`)
- `$eventContext.` (projections)
- PDL quick reference (most forms are compiled in `pdl-example.md` or `cheat-sheet-example.md`; `remove via join` and `$eventContext.occurred` are not)
- Specification forms

Released boundary forms: `public event X`, `event X from "store"`, Translate
`direction inbound|outbound`, event-target `projection/reducer P => PublicEvent`,
and capture `source events` with `from ForeignEvent` select v9. Read
[public contracts](../../cratis-screenplay-automations-and-translations/references/public-events-and-direction.md).
View properties use trailing `key`; multipart `by` query/read blocks remain
PLAY0268 (#599), not v10. Read [named keys](../../cratis-screenplay-read-surface/references/named-keys.md).
Production `stream Source.Stream` overrides and reaction/reducer `from Source[.Stream]`
select released v10: [routes and filters](production-routes-and-filters.md).
Qualifying non-route Double literals select published v11 at 4.135.0;
`numbers exact` does not bind. Non-positional enum member numbers (`draft = 1`)
select published v12 at 4.139.0; positional pins keep earlier bytes/version,
while specifications still use names. The pinned CLI/Stage pair (`versions.md`) admits v12 and renders explicit numbers.
Syntax/example: command-surface `references/enum-numbers.md`.
See `versions.md` for freeze and numeric boundaries.

Current pins: `versions.md` (the single table). Compile evidence below keeps its historical tool labels.

The syntax lives in compiled example files next to this one (each is a Markdown file holding
one complete `screenplay` fence). Copy shapes from them; do not write a form that none of
them (or a compiled documentation example) shows. This file holds the prose rules; the
examples belong to `cheat-sheet-example.md` and its siblings.

| File | Mode | V1, warnings as errors | V3 (binds) |
| --- | --- | --- | --- |
| `cheat-sheet-example.md` | design: every construct family | 0 diagnostics, both tools (cratis before 3.28.2 reported a false PLAY0285 on the `RecordingAPayment` cascade spec) | not ready by design (its header lists why; fewer blockers on the standalone tool) |
| `executable-example.md` | executable, StateChange and StateView | 0 diagnostics, both tools | ready, both tools |
| `pdl-example.md` | executable projections | 0 diagnostics, both tools | ready, both tools |
| `automation-translate-example.md` | executable Automation and Translate (ESM v6) | 0 diagnostics, both tools (cratis before 3.28.2 could not bind it) | ready, both tools (cratis 3.28.2 or later) |

Screenplay sources: `Documentation/screenplay/grammar.md` (EBNF; checked against the parsers
by `for_Documentation/when_comparing_the_grammar_against_the_parsers.cs`),
`projections/grammar.md`, `captures/grammar.md`. Documentation examples compile in
Screenplay CI (`when_compiling_every_example.cs`) against repository main: the standalone
compiler (4.68.0; `cratis` 3.28.2 or later bundles 4.66.0, which lacks ESM v7); older cratis bundles differ (`versions.md`).

## Samples and preview fixtures

Invoicing is a warning-free syntax showcase, not a runnable whole-language proof.
The released samples policy excepts these preview families and points to dedicated
fixtures; do not infer binder admission from the sample's coverage list:

| Family | Screenplay v4.125.0 fixture |
| --- | --- |
| Exact numbers | `Source/Screenplay/Compiler/Conformance/exact-named-rule-intent.play` |
| Sources/streams/command routes | `Documentation/screenplay/fixtures/source-streams.play` |
| Specification routes | `Source/Screenplay/Compiler/Conformance/specification-streams.play` |
| Systems/operations/specification steps | `Documentation/screenplay/fixtures/operations.play` |
| Refusals/redelivery/no-events | `Source/Screenplay/Compiler/Conformance/reaction-refusals-redelivery.play`, `no-events.play` in that directory |

Source: `v4.125.0:.cratis/ai/rules/project/samples.md`. Its preview list retains
sources/specification routes, but the released binder/runner admits them as v8;
its old blanket PLAY0268 wording is not the current route verdict. Other rows
remain unadmitted. Compile and test each relevant fixture with the chosen tool.

## Lexical and structural rules
- Spaces only, 2 per level (tabs: PLAY0006 W). Offside rule.
- Comments: `//` only. `#` is not a comment (PLAY0058 E in a projection) despite doc examples.
- Body directives can appear in any order; singleton `description` still appears
  at most once. Canonical printing may place it first, but the parser does not
  require that position. Source preambles precede declarations.
- Strings: `"..."`, escapes `\" \\ \n \r \t`; other backslashes kept (regex `"^INV-\d{6}$"` works).
- Structured values: single-line JSON, quoted keys (`[{"sku": "A-1", "quantity": 2}]`).
- One condition grammar (policy `require`, `produces when`, `require`, reaction `where`): `and`
  binds tighter than `or`; operators `== != > >= < <= contains`, `starts with`; claims use
  `claim "x" matches "v"` (not `==`: PLAY0120).
- Keyword escape `@name` for properties named `tag`, `authorize`, `produces`, `reads`, `file`,
  `validate` (enum value), `sequence`, `occurred`, `causedBy`, `namespace`, `correlation`,
  `causation`, and projection `key`/`parent`/`with`.
- `type` and `concept` are top level only (inside a slice: PLAY0029, silently dropped).
- Concept rules have no subject: `not empty`, `min 3`, `max 10`, `length == 10`,
  `matches email`, `matches "regex"` (`length 3 to 10` / `length >= 3`: PLAY0141).
- Property modifiers: `Type[]`, `Type optional`, `Type[] optional`; order `Type optional
  generated identifier` (PLAY0484); legacy `?` is PLAY0479 (information).
- Ownership policies: `require claim "ownerId" matches subject` (subject = command identifier
  or keyed query argument) or `matches <commandProperty>`; executable; renderable only for
  text-backed targets at the 4.24-era Stage; Stage 4.73.1 also renders Uuid and Date claim targets (see `renderable-subset.md`; unsupported subjects: STAGE-ESM-015).

## Exact numeric source mode

Start each file/import barrel with `numbers exact` to retain bounded decimal
literals without Double rounding. All assembled files must agree on mode;
PLAY0508–PLAY0513 diagnose malformed directives, range errors and disagreement.
Exact mode is authoring-only (`PLAY0268` on binding), not ESM v7 generated values.
Typed JSON needs consistent `sourceOptions.numericMode: "exact"` on every root
and `ExactNumber` literal envelopes with canonical fixed-point string values,
never JSON numbers. Details: model-authoring `references/language-reference.md`.

## Clause order inside a command (canonical)
description, properties, `reads`, `authorize`, `validate`, `produces`/`handler`, `concurrency`.

## Slice contents (typical)
- `slice StateChange <Cmd>`: command, its events, constraints, dialog screen, success/rejection/denial specs.
- `slice StateView <View>`: readmodel, keyed query (+ design-mode list query), projection or reducer, screen, view specs.
- `slice Automation <Behaviour>`: reaction(s) + events they produce + specs.
- `slice Translate <Behaviour>`: capture and/or translator reaction + both vocabularies' events.
- Declare each event once, in the slice that produces it; reference it by name elsewhere.

## `$context.` paths (command `produces`)
`occurred`, `tenant`, `command.<p>`, `arguments.<p>`, `causedBy.subject|name|userName`,
`causation.type`, `identity.id|name|userName|isAuthenticated|roles|claims.<n>`.
Binder-supported (`BindOccurrence`, v4.66.0): `occurred`, `identity.id` (= `causedBy.subject`),
`identity.name` (= `causedBy.name`), `identity.userName` (= `causedBy.userName`). `tenant`,
roles, claims, causation, `command.<p>`, `arguments.<p>`: PLAY0268. Binding is not execution:
a reference scenario that supplies only a clock carries no caller audit identity, so check the
reference runner before relying on identity paths in a specification. Renderable:
`$context.occurred` only.

At 4.127.0, `$identity.id`/`.name`/`.userName` map to the same ESM kinds as
`$context.identity`; prefer the shorter root, but the older one stays supported.
Other built-ins parse but roles/claims/authentication status do not bind in
produces mappings. Identity details are metadata: executable detail reads report
PLAY0268. See command-surface `references/context.md`.

Use bare `pii`/`secret` on concepts (`personal` aliases `pii`); legacy markers
are PLAY0565 information and repeated markers are PLAY0653 warning. Event `subject`
is report-only lineage; processing purposes/checks/reports declare use, not lawful
processing or runtime retention. See command-surface `references/compliance.md`.

## `$eventContext.` (projections)
`eventType{.id,.generation,.tombstone}`, `eventSourceType`, `eventSourceId`, `eventStreamType`,
`eventStreamId`, `sequenceNumber`, `occurred`, `occurred.Week`, `eventStore`, `namespace`,
`correlationId`, `causedBy{.subject,.name,.userName,.onBehalfOf}`, `hash`, `subject`,
`subjectIsEventSourceId`, `observationState` (unstable on replay). `causation` and `tags` are
collections (path below them: PLAY0297 E). Unknown member: PLAY0295/0296 W.
`$eventSourceId` is the short form used in mappings. `occurred.Week` compiles but does not bind
(PLAY0273: derived value). Any `$eventContext` path other than `eventSourceId` binds, but the execution plan refuses it ("An event-context value other than the event source identity needs occurrence context that ESM v1 facts do not carry"), so a model that must run uses `$context.occurred` in the command `produces` instead (it executes when the scenario has `given clock`). Catalogue: `Documentation/screenplay/projections/event-context.md`.

## PDL quick reference (most forms are compiled in `pdl-example.md` or `cheat-sheet-example.md`; `remove via join` and `$eventContext.occurred` are not)
- Header `projection Name => ReadModel`; variant group `projection Name` + `variant RM` /
  `enters on Event [key k]`.
- Directives: `no automap` (AutoMap is on by default; scoped projections match names case-insensitively for the same type, flat-bound ones exact names only), `every`
  (mappings applied for every event), `all` (per event source, never global), `from Event [key k]`.
- Mappings: `target = source`, `"literal"`, `$eventSourceId`, `$eventContext.occurred` (binds, but blocks the execution plan),
  `increment n`, `decrement n`, `count n` (in `all`), `add t by x`, `subtract t by x`,
  `set t to "v"`, `clear t`.
- `join Label on rmProperty` + `with Event` (matches the joined event's source id against the
  property; the label is decoration; joins never create instances).
- `children prop identified by k` + `from E key k` + `remove with E key k`; `nested prop` +
  `from` + `clear with E`.
- Removal: `remove with Event`; `remove via join on Event` is compile-only and bind-only: at projection level it blocks the whole reference execution plan (`UnsupportedProjectionBlock`), so no specification runs.
- Keys: only on `from` (a projection-level `key` routes nothing: PLAY0381 W).
- One `every`/`all` block per level for the executable model (else PLAY0268).
- Templates (`` `${a}` ``) and `$causedBy` parse but do not bind (PLAY0268).

## Specification forms
Full list with semantics: `Documentation/screenplay/specifications.md`. Compiled in the
examples: `given caller` (+`authenticated`, `role`), `given clock`, `given <Event> for`,
`when <Command>`, `when append <Event> for`, `then <Event> for`, `then error "msg"`,
`then denied`, `then query ... arguments ... result`, `then no readmodel RM for "key"`.
`then` events are exhaustive and ordered; after `when append` (ESM v6) they list only what
followed it. Duplicate and competing-claim checks use `when <Command>` with the earlier fact
as `given`. Specification construct depth: `cratis-screenplay-specifications`.
