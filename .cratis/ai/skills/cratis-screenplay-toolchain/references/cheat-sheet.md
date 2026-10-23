<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/cheat-sheet.md -->
# Screenplay cheat-sheet

## Contents

- Validation commands and the gate file
- Samples and preview fixtures
- Lexical and structural rules
- Exact numeric source mode
- Clause order inside a command (canonical)
- Slice contents (typical)
- `$context.` paths (command `produces`)
- `$eventContext.` (projections)
- PDL quick reference (most forms are compiled in `pdl-example.md` or `cheat-sheet-example.md`; `remove via join` and `$eventContext.occurred` are not)
- Generated values and proofs
- Specification forms

Released boundary forms: `public event X`, `event X from "store"`, Translate
`direction inbound|outbound`, event-target `projection/reducer P => PublicEvent`,
and capture `source events` with `from ForeignEvent` select v9.
For event captures, present `given <FromEvent>` / `when append <FromEvent>`, not
capture records (PLAY0701). The key must be required on every `from` event, never
a context key (PLAY0700). PLAY0701 repairs only an unambiguous single-`from` capture;
PLAY0700 has no automatic repair.
Requires Screenplay 4.171.0 or later (`screenplay --version`); older runners do not
deliver these events to captures.
A directionless Translate reacting without a `from` route to its own capture's
append warns PLAY0694 (fails `--warnaserror`): move the reaction to Automation,
keeping the capture in Translate. No automatic split.
Requires Screenplay 4.176.0 or later; older compilers leave that shape unchecked.
Read
[public contracts](../../cratis-screenplay-automations-and-translations/references/public-events-and-direction.md).
View properties use trailing `key`; several parts select v27 (not v10), with a `by` block on
an optional single-instance query and equality filters on lists. Read [named keys](../../cratis-screenplay-read-surface/references/named-keys.md).
Production `stream Source.Stream` overrides and reaction/reducer `from Source[.Stream]`
select released v10: [routes and filters](production-routes-and-filters.md).
Qualifying non-route Double literals select v11;
`numbers exact` does not bind. Non-positional enum member numbers (`draft = 1`)
select v12; positional pins keep earlier bytes/version,
while specifications still use names. Stage admits v12 and renders explicit numbers.
Syntax/example: command-surface `references/enum-numbers.md`.
Published ESM versions never change meaning; later byte-changing features take a new version.

The syntax lives in compiled example files next to this one (each is a Markdown file holding
one complete `screenplay` fence). Copy shapes from them; do not write a form that none of
them (or a compiled documentation example) shows. This file holds the prose rules; the
examples belong to `cheat-sheet-example.md` and its siblings.

| File | Mode | V1, warnings as errors | V3 (binds) |
| --- | --- | --- | --- |
| `cheat-sheet-example.md` | design: every construct family | 0 diagnostics, both tools (an older bundled compiler reported a false PLAY0285 on the `RecordingAPayment` cascade spec) | not ready by design (its header lists why; fewer blockers on the standalone tool) |
| `executable-example.md` | executable, StateChange and StateView | 0 diagnostics, both tools | ready, both tools |
| `pdl-example.md` | executable projections | 0 diagnostics, both tools | ready, both tools |
| `automation-translate-example.md` | executable Automation and Translate (ESM v6) | 0 diagnostics, both tools (an older bundled compiler could not bind it) | ready, both tools |

Screenplay sources: `Documentation/screenplay/grammar.md` (EBNF; checked against the parsers
by `for_Documentation/when_comparing_the_grammar_against_the_parsers.cs`),
`projections/grammar.md`, `captures/grammar.md`. Documentation examples compile in
Screenplay CI (`when_compiling_every_example.cs`) against repository main: the standalone
compiler; the compiler bundled in `cratis` can be older and lack the newest ESM versions.

## Validation commands and the gate file

```shell
screenplay <root> --warnaserror --no-color      # V1; also reads <root>/screenplay.json
screenplay <root> --print-gate                  # effective gate and each value's source, no compile
screenplay <root> --format json                 # one JSON report: diagnostics, gate, exitCode
screenplay <root> --no-gate-file --check all    # bypass the file for one run
screenplay test <root>                          # V4
```

`screenplay.json` (authored, beside `.screenplay/`) holds `validation.checks`, `warningsAsErrors`, `scope`
and `openQuestions`; unknown keys or check names are refused (exit 2), never ignored. Details and
precedence: [verdicts](verdicts.md).

## Samples and preview fixtures

Invoicing is a warning-free syntax showcase, not a runnable whole-language proof.
The released samples policy excepts these preview families and points to dedicated
fixtures; do not infer binder admission from the sample's coverage list:

| Family | Screenplay fixture |
| --- | --- |
| Exact numbers | `Source/Screenplay/Compiler/Conformance/exact-named-rule-intent.play` |
| Sources/streams/command routes | `Documentation/screenplay/fixtures/source-streams.play` |
| Specification routes | `Source/Screenplay/Compiler/Conformance/specification-streams.play` |
| Systems/operations/specification steps | `Documentation/screenplay/fixtures/operations.play` |
| Refusals/redelivery/no-events | `Source/Screenplay/Compiler/Conformance/reaction-refusals-redelivery.play`, `no-events.play` in that directory |

The Screenplay samples policy lists these preview families. Sources and specification routes
are admitted as v8 by the binder and runner, so a blanket PLAY0268 is not their verdict. Other rows
remain unadmitted. Compile and test each relevant fixture with the chosen tool.

## Lexical and structural rules
- Spaces only, 2 per level (tabs: PLAY0006 W). Offside rule.
- Comments: `//` only. `#` is not a comment (PLAY0058 E in a projection) despite doc examples.
- Body directives can appear in any order; singleton `description` still appears
  at most once. Canonical printing may place it first, but the parser does not
  require that position. Source preambles precede declarations.
- Strings: `"..."`, escapes `\" \\ \n \r \t`; other backslashes kept (regex `"^INV-\d{6}$"` works).
- Structured values: single-line JSON, quoted keys (`[{"sku": "A-1", "quantity": 2}]`).
- Literal property/concept text rules use `contains`, `not contains`, `starts with`,
  `ends with` with quoted operands (v24, ordinal UTF-16). Portable `matches` refuses
  nonportable constructs with PLAY0699; exact Studio patterns warn PLAY0787.
  Read command-surface [text and match rules](../../cratis-screenplay-command-surface/references/text-and-match-rules.md).
- One condition grammar (policy `require`, `produces when`, `require`, reaction `where`): `and`
  binds tighter than `or`; operators `== != > >= < <= contains`, `starts with`; claims use
  `claim "x" matches "v"` (not `==`: PLAY0120). Text operators here are condition-only
  authoring syntax (PLAY0268 at binding), not the admitted property text rules.
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
  text-backed targets, Uuid and Date claim targets (see `renderable-subset.md`; unsupported subjects: STAGE-ESM-015).

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
Binder-supported: `occurred`, `identity.id` (= `causedBy.subject`),
`identity.name` (= `causedBy.name`), `identity.userName` (= `causedBy.userName`). `tenant`,
roles, claims, causation, `command.<p>`, `arguments.<p>`: PLAY0268. Binding is not execution:
a reference scenario that supplies only a clock carries no caller audit identity, so check the
reference runner before relying on identity paths in a specification. Renderable:
`$context.occurred` only.

`$identity.id`/`.name`/`.userName` map to the same ESM kinds as
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
(PLAY0273: derived value). Any `$eventContext` path other than `eventSourceId` binds, but the reference runner refuses it for the specifications that reach that projection ("An event-context value other than the event source identity needs occurrence context that ESM v1 facts do not carry"), so a model that must run uses `$context.occurred` in the command `produces` instead (it executes when the scenario has `given clock`). Catalogue: `Documentation/screenplay/projections/event-context.md`.

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
- Removal: `remove with Event`; `remove via join on Event` is compile-only and bind-only: at projection level it makes only the specifications that reach that projection unsupported (`UnsupportedProjectionBlock`); the others run (Screenplay 4.213.2 or later; earlier compilers refuse the whole plan).
- Keys: only on `from` (a projection-level `key` routes nothing: PLAY0381 W).
- One `every`/`all` block per level for the executable model (else PLAY0268).
- Templates (`` `${a}` ``) and `$causedBy` parse but do not bind (PLAY0268).

## Generated values and proofs

Choose readable invoice/order references with `generate pattern "INV-{yyyy}{MM}-#{7}" checksum luhn`
on a String concept and author `validate checksum luhn` separately. `#`, `?`, `*`
mean digit, uppercase letter, letter-or-digit; ASCII classes and repeats up to
64 are finite. UTC segments are `{yyyy}`, `{yy}`, `{MM}`, `{dd}`. Int
`generate random 100000..999999` is inclusive; `generate rule Name` attaches one
file/tagged fence with Now/Tenant context and unknown entropy. These are ESM v21.
Requires Screenplay 4.172.0 (`screenplay --version`) for these constructs; older compilers report PLAY0268.
Sequence and generated secrets remain refused.

Generated-concept rules select v20 (older compilers refuse them with PLAY0268): Proven silent, Refuted PLAY0688 plus witness, Unproven PLAY0689
plus reason. Finite support exhaustion is PLAY0691, empty support on a scanned
date PLAY0692, not a proof warning. MCP rules[]/derivedPattern support typed
matches/PLAY0688 repair proposals.
Requires Screenplay 4.173.0 for those MCP fields; older servers omit them.
See [generation example](generation-strategies-example.md).

Pin whole identifiers with `for`, other values with named `generated`; pins obey
shape and rules. `given clock` fixes all UTC pattern date segments, not randomness;
without it only digit widths are checked. Patterns are not gapless sequences or
secrets; check digits catch typos, not forgery. Short payload codes need uniqueness,
generated source destinations need freshness. Read
[Stage admission](../../cratis-stage-rendering-and-sandbox/references/admission.md)
for v21 admission, all-Proven unpinned specs and the separate prerequisites for
warning/info rules, checksum validation and pattern/random rendering.
`generate rule` renders from Stage 4.136.0 (STAGE-ESM-028 in the Host). Reference binding is not rendering.

## Specification forms
Full list with semantics: `Documentation/screenplay/specifications.md`. Compiled in the
examples: `given caller` (+`authenticated`, `role`), `given clock`, `given <Event> for`,
`when <Command>`, `when append <Event> for`, `then <Event> for`, `then error "msg"`,
`then denied`, `then query ... arguments ... result`, `then no readmodel RM for "key"`.
`then` events are exhaustive and ordered; after `when append` (ESM v6) they list only what
followed it. Duplicate and competing-claim checks use `when <Command>` with the earlier fact
as `given`. Specification construct depth: `cratis-screenplay-specifications`.
