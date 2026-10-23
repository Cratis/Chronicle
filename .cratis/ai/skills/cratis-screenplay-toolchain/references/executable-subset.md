<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/executable-subset.md -->
# Executable subset (what binds: V2 and V3)

## Contents

- Caller and compliance boundary
- Admitted on both compilers
- Text `not empty` reference correction
- Child-before-parent reference execution
- Equality query filters (ESM v14)
- Time, reactions and captures (ESM v6)
- Generated values and responses (ESM v7)
- Released public boundaries and observer routes
- Chronological command date comparisons (ESM v26)
- Flat composite read-model keys (ESM v27)
- Uuid equality in command conditions (ESM v28)
- Code attachments (what binds, what never does)
- Refused (code, reason): fix, or record as a mode gap
- Not refused, but do not rely on it
- Never do this to reach V3

What the binder admits, and what it refuses with which code, depends on the compiler: the
standalone tool binds the newest ESM versions, while the compiler bundled in the CLI (and the
renderer that uses it) admits through an older version. Operations, the `numbers exact`
**source mode** (PLAY0268) remain unadmitted. Routes bind as
ESM v8, public/direction constructs as v9, identity/overrides/filters as v10, qualifying
non-route Double literals (exact lowering of Double-mode literals) as v11, non-positional enum
numbers as v12, concept compliance as v13, caller-supplied collection equality filters as v14,
concept generation strategies and freshness as v15, stored event names as v16,
no-event assertions/redelivery as v17, query-error specifications as v18,
invocation refusal branches as v19, generated-concept rules as v20, and finite
pattern/random/rule generation plus checksum validation as v21. Always name
the tool with a V2 or V3 result. When tool and documentation disagree, the tool decides
current capability; the documentation and the domain decide intended correctness. Record
the gap, never strip the model to pass. Use the actual binder before promoting an older
observation to a capability claim.

Binding is observed through MCP (`read-workspace view=executable-diagnostics`), a render, or
`cratis screenplay validate --executable <root>` (bundled compiler; binding errors only, writes
nothing; exit 5 when it does not bind). `screenplay <folder>` and plain `cratis screenplay validate`
never bind (`verdicts.md`).

Complete examples that bind live in `executable-example.md`, `pdl-example.md` (projections),
`automation-translate-example.md` (ESM v6) and `generated-responses-example.md` (ESM v7).

For atomic batches (v22), described handlers (v23), ordinal text rules (v24) and
protected reads (v25), chronological command dates (v26), composite keys (v27) and Uuid equality (v28), read
[language admission](language-admission.md). A later
version alone does not enable either application opt-in. The language is frozen at ESM v1–v28 ([the freeze decision](https://github.com/Cratis/Screenplay/blob/main/decisions/0092-the-language-is-frozen-at-esm-v28.md)): no new versions, constructs, keywords or grammar, except a construct for a real model that is blocked with no model-side alternative, accepted in its own record. Model with what v1–v28 offers; when Stage, `cratis render` or Studio does not support a construct yet, treat the typed refusal as a platform gap to report, since consumers are catching up to v28, not a reason to wait for new language.

## Caller and compliance boundary

`$identity.id`/`.name`/`.userName` are aliases of the admitted
`$context.identity` produces-mapping values. Authentication status, roles and
claims still refuse there (PLAY0268); the older root is not deprecated.
A top-level `identity` block alone is authoring metadata and does not block
readiness. Executable reads of declared `$identity.<detail>` do (PLAY0268);
`$context.identity` remains built-ins only. Purposes and event `subject` marks
are metadata (PLAY0270), not source resolution, retention or subject propagation.
Bare `pii`/`personal` and `secret` concepts, scopes and qualifiers bind as v13. Enum/identity
compliance and events mixing protected values with explicit subject marks remain
refused; see command-surface `references/compliance.md` for the precise boundary.
Legacy spellings compile with PLAY0565 information; duplicates warn PLAY0653.
Never remove protection. Reference execution proves neither encryption nor erasure.

Requires Screenplay 4.141.0 or later (`screenplay --version`) to bind concept compliance (ESM v13); older compilers refuse it with PLAY0268.

## Admitted on both compilers

- `StateChange` and `StateView` slices.
- A command with one `identifier`; inline `produces event <Name>` (omit `for`) for a command-owned event, or
  plain `produces <Event>` with explicit `for <identifier>` for a shared event.
- `produces when` with `==`/`!=` on scalar text, enum, number, boolean or Uuid (v28), and ordering on
  numbers.
- Mappings from command properties, string and number literals, `$context.occurred`,
  `$context.identity.id`, `.name` and `.userName` (aliases `causedBy.subject`, `.name`,
  `.userName`). Binding is not execution: a clock-only reference scenario supplies no caller
  audit identity.
- Concept and command rules: `not empty`, `min`, `max`, `length ==`, `matches`, numeric
  comparisons, `all >`/`all >=` on numeric collections, `require <command-property
  condition>`, `severity`. The operand is a literal, never a property reference.
- Declarative policies (`require role`, `authenticated`, `claim ... matches`) on module,
  feature, command and query. Ownership policies `claim "x" matches subject` or `matches
  <commandProperty>`.
- `constraint ... unique <prop> on <Event>`. Admission is separate from the open Chronicle
  durability defects: never treat admission as proof of uniqueness.
- A `readmodel` with one explicit trailing `key`, or an unambiguous inferred key:
  single-instance queries in the owning slice agreeing on `by xId XId`; only if
  none supplies identity and an admitted list query exists does conventional
  `*Id` inference run (collection-key exclusions: read-surface `references/named-keys.md`).
  Queryless read models need an explicit `key`. Explicit single keys preserve
  equivalent inferred ESM bytes. Flat composite read-model keys bind as v27
  (see below). Collections and observable delivery bind.
  Caller-supplied equality filters on collections also bind as ESM v14; context
  sources, single-result filters, scope and a list-query `by` block remain blocked; a `performer` query is an opaque read left out of the executable model.
- One projection per read model: `from`, `every`, `all` (one block per level), `join`,
  `children`, `nested`, `remove with`, `remove via join` (binds; blocks the execution plan at the projection level, see below), `clear with`, counters, `add`,
  `subtract`, `set`, `clear`, variants, and the identifier mapped from the projection's key (`xId = $eventSourceId` only for an event-source key).
- Specifications: `given caller`, `given <Event> for`, `when <Command>`, `when append
  <Event> for`, `then <Event> for`, `then error "msg"`, `then denied`, `then query ...
  arguments ... result`, `then no readmodel RM for "key"` (ESM v5), and when-less view
  specifications ending in `then query` or `then readmodel`.
- `seed`. Personas, screens and descriptions are PLAY0270/PLAY0269 information, not
  blocking.

## Text `not empty` reference correction

Concept and command property `not empty` reject null, empty and whitespace-only
text in reference execution, using Unicode `White_Space` (.NET `char.IsWhiteSpace`). U+0085 and U+00A0
count; U+FEFF does not. No trimming/normalization or canonical-byte/ESM-version
change, in every ESM version. Specifications that relied on whitespace being accepted now fail.
Collections still require only one element, and event-source/stream-id identity
text still treats whitespace as a value. Stage and Arc target behavior is separate; run
their own specs to establish parity. Complete passing model:
[whitespace rules](../../cratis-screenplay-specifications/references/whitespace-rules.md).

Requires Screenplay 4.151.0 or later (`screenplay --version`) for the whitespace-only rejection; earlier compilers accept whitespace-only text.

## Child-before-parent reference execution

An admitted child projection event before its parent is deferred, not Unsupported.
The reference evaluator retries pending children in arrival order with original event
values/context and parent keys, including grandchildren and across given/when and
accepted command commits. A parent that never arrives leaves the child absent;
an ambiguous parent still fails closed. Pin child-before-parent, parent-before-child
and absence with [child-ordering specifications](../../cratis-screenplay-specifications/references/child-ordering.md).
For shared root/child events the evaluator uses the current root handler key, not
a child's location resolved through a different creation event. This is reference
semantics, not real Chronicle runtime conformance.

Requires Screenplay 4.149.1 or later (`screenplay --version`) for deferral; earlier compilers report such a child as Unsupported.

## Equality query filters (ESM v14)

`filter <name> <Type> [optional]` binds on collections with no `from`, targeting a
same-named scalar primitive/concept/enum property of the same nominal type
(ignoring optionality). Distinct filter names must not repeat `by`. All supplied
filters AND with `by`; optional omission does not narrow, supplied null is refused.
`then query … arguments` and `when query` accept both `by` and filters; PLAY0468
reports unknown/repeated/missing required arguments. Collection results match by
read model/key regardless of row order. Stage admits and renders these filters, subject to
construct checks (DateTime filters and parameter order still refuse). Complete compiled specs:
read-surface [equality filters](../../cratis-screenplay-read-surface/references/equality-filters.md).

Requires Screenplay 4.143.0 or later (`screenplay --version`) for equality filters; earlier compilers refuse them.

Requires Screenplay 4.105.0 or later (`screenplay --version`) for list queries (unfiltered, or narrowed by one `by` key); earlier compilers refuse them with PLAY0268.

## Time, reactions and captures (ESM v6)

- `slice Automation` with `reaction`: `when <Event>` (with values) then `produces` (no
  `for` means the trigger's event source) or `invokes <Command>`; `where`; clock `at ... on
  ...` with an explicit `for`. Without a declared actor, a gated invocation denies.
  Reaction `runs as system role "<Role>"` supplies the actor and
  selects ESM v10; keep the gate. Stage renders role/authenticated reaction identity; claim-reading/opaque gates retain STAGE-ESM-034.
  Standalone reference execution admits it.
- Application `trigger` declarations and `when trigger` specifications.
- `slice Translate` with `capture` (`key`, `map ... translate`, `append ... when x from "a"
  to "b"`), and `given capture` / `when capture` specifications.
- `given clock` and `when clock`. The clock is UTC and exact; an occurrence fires if it is
  due **after** the given clock and **at or before** the when clock, once each, in time
  order. `when clock` needs `given clock`. Limits: 10,000 occurrences per advance, 1,000
  facts per scenario. A scenario with given equal to when fires nothing.
- Command specifications that list reaction cascades.
- In a `then` list after `when append`, only what followed the appended event.

Requires Screenplay 4.61 or later (`screenplay --version`) for ESM v6 constructs, and Cratis CLI 3.28.2 or later for the bundled compiler; older compilers refuse them with PLAY0268 and report a false PLAY0285 on cascades.

Requires Screenplay 4.125.0 or later (`screenplay --version`) for reaction `runs as system`.

## Generated values and responses (ESM v7)

A model selects ESM v7 only when it uses a generated property, a response, a generated fixture or a `then returns`
expectation; every other model keeps its version, bytes and revision. The CLI bundle binds these constructs
and Stage renders supported UUID/String strategies, v15 freshness, scalar/named responses,
fixtures and `then returns`. Unsupported generated types (`-028`) and result combinations
(`-038`) remain refused; see `renderable-subset.md` and command-surface
`references/generated-values-and-responses.md`.
Complete examples: `generated-responses-example.md`, `generation-strategies-example.md`.

- A generated property is command-only, required, scalar and a generatable concept backed by Uuid/String,
  or Int with `random` (v21). Bare primitives, optional and collection types are invalid.
  Uuid has implicit `uuid`; String needs a strategy. Generated identifiers remain Uuid/String only.
  Explicit serialized generation selects v15, with strict fixture encoding and whole-source freshness for all
  generated destinations. Sequence and generated secrets remain refused.
  Generated-concept rules bind as v20 with proof diagnostics PLAY0688/0689;
  pattern/random/rule and checksum validation bind as v21. PLAY0691 is uncountable
  support within budget; PLAY0692 is empty support on a scanned UTC date.
  See command-surface `references/generated-values-and-responses.md` and `generation-strategies-example.md`. Modifier order is `Type optional generated identifier`.
- Generated values are not request inputs, form fields, invocation arguments or ordinary `when` values. Generation
  runs after authorization and validation and before productions, so a denial or a validation failure never
  reaches it.
- A policy (including inherited or composed ones and the implicit `subject` of a generated identifier), a property
  rule or a requirement that references a generated value reports `PLAY0273`; reference input properties only. A
  generated property whose concept declares rules selects v20; refuted rules report
  PLAY0688 with a witness and Unproven rules warn PLAY0689 with a reason. Proven
  rules bind silently; matches containment requires deterministic patterns, not
  passing samples. Opaque validation needs a target. Opaque generation rules use
  pinned String results without executing their bodies.
  A reaction mapping into a generated property is invalid.
- Responses: `returns <property>` is scalar; bare `returns` opens an ordered record of `<name> [<Type>] = <property>` fields
  over direct properties of the same command (collections, read aliases, arithmetic and whole read models are not
  supported). A response exists only on acceptance; a later reaction or scenario-query failure keeps the accepted facts and
  no response. No response differs from a response whose optional source is `Null`.
- A two-token `returns name` is a response when `name` is another property of this command; otherwise it declares a
  property named `returns`. Use `returns @name` or `@returns Type` to force one reading.
- Specifications: `when ... for "<uuid>"` supplies the generated identifier, an indented `generated <name> = <value>`
  any other generated value, and `then returns` asserts the response (scalar value, or a non-empty named subset of a
  record). A reached generated value without a fixture is `Unsupported(IdentityAllocation)`; the specification never
  passes. `then returns` is a success outcome but does not relax the events comparison.
- `for` on a generated identifier does not assert that explicitly routed productions target it. A plain `produces`
  without `for` keeps its own allocation channel, which a fixture never satisfies; a generated identifier combined
  with a reached plain production is `Unsupported(IdentityAllocation)` from source specifications.
- A reaction-invoked command that reaches generation is `Unsupported(IdentityAllocation)`; response-only commands run
  and discard the response. Generated values give no idempotency, retry or deduplication guarantee.

Requires Screenplay 4.68.0 or later (`screenplay --version`) for generated values and responses (ESM v7), 4.147.0 for strategies/freshness (v15), 4.164.0 for generated-concept rules (v20), and 4.172.0 for pattern/random/rule/checksum (v21); older compilers refuse the respective constructs with PLAY0268.

Pins satisfy both strategy shape and concept rules; failure is Rejected(Contract),
not an input error or redraw. Pin date segments with `given clock`; without it the
runner checks digit widths only. Generation checksum does not add validation:
author `validate checksum` for arithmetic. Requires Stage 4.109.0 for v20,
4.115.0 for v21 admission/all-Proven unpinned specs, 4.117.0 for warning/info rules,
4.118.0 for checksum validation and 4.119.0 for pattern/random rendering; older
renderers refuse those constructs or require rule-bearing pins even when Proven.
Unproven/Refuted rules still need pins (-038), bad pins fail -011, and
`generate rule` renders from Stage 4.136.0 (the Host and legacy syntax engine keep -028). Do not remove rules or classification to render.

## Released public boundaries and observer routes

Public/origin events, explicit Translate direction, event-target builders and
`source events` captures bind as ESM v9. Publication folds execute, and so do
captures over events (Requires Screenplay 4.171.0; a specification presents the
consumed event with `given <Event>` / `when append <Event>`); reached opaque
event-target reducers return typed Unsupported. Public/foreign imports still lack a local shape and refuse PLAY0268.
See automations-and-translations `references/public-events-and-direction.md`.
Production overrides and reaction/reducer filters bind and select v10;
filters match routed source/stream names only. Read
[production routes and filters](production-routes-and-filters.md).
Qualifying retained non-route Double literals bind/reference-execute as v11,
unlike `numbers exact`.

Non-positional enum member numbers bind as ESM v12; positional
pins preserve the earlier executable version/bytes. The optional canonical last
`numbers` array parallels `values`; specifications still use member names.
Consumers must admit v12 explicitly; Stage does. Syntax:
command-surface `references/enum-numbers.md`.

Concept `pii`/`personal` and `secret`, qualifiers and scopes bind as optional
`compliance` in ESM v13. Reasons remain
report-only PLAY0270; reference execution proves neither encryption nor erasure.
Enum/identity concepts and an event combining personal or explicitly subject-scoped
secret values with an explicit `subject` mark refuse PLAY0268 at binding only.
Stage renders v13 concept protection; protected identities/enums still refuse
`STAGE-ESM-040`. Preserve protection, not a falsely green bind.

Requires Screenplay 4.117.0 or later (`screenplay --version`) for ESM v9, 4.129.0 or later for production overrides and observer filters (v10), 4.135.0 or later for v11 and 4.139.0 or later for enum numbers (v12).

## Chronological command date comparisons (ESM v26)

Requires Screenplay 4.199.0 (ESM v26; `screenplay --version`); older binders refuse
with PLAY0268. `==`, `!=`, `>`, `>=`, `<`, `<=` bind for Date/Date or DateTime/DateTime.
`require`/`produces when` compare required scalar command properties to properties
or literals; command/property validation rules accept literal right operands only.
DateTime compares UTC instants at tick precision, not text. Every participating
DateTime input needs an offset or `Z`, else Validation rejects before rules, even
for skipped comparisons. `then` matching stays ordinal. Invalid date literals or
offset-less DateTime literals in admitted comparisons report PLAY0273. Read
[date comparisons](../../cratis-screenplay-command-surface/references/date-comparisons.md)
for the complete window model and retained PLAY0268 refusals. Requires Stage 4.141.0
for rendered date comparisons; older renderers refuse STAGE-ESM-046. Read
[rendered dates](../../cratis-stage-rendering-and-sandbox/references/date-comparisons.md)
when checking remaining refusals and input-binding differences. Standalone binding/reference
execution is independent of rendering.

## Flat composite read-model keys (ESM v27)

Requires Screenplay 4.204.0 (ESM v27; `screenplay --version`); older binders refuse a
read model with two or more `key` marks with PLAY0268. Binding admits a model with
several flat scalar key parts when:

- root `from` and `remove with` blocks name every part in an untyped `key` block;
- a list query narrows with equality filters, and an optional single-instance query
  names every part in a `by` block;
- fixtures state every part (`given readmodel`, `then readmodel`, `then no readmodel`).

Models with one `key` mark keep their earlier version and bytes. Refused with PLAY0268:
joins, `children`, `remove via join`, `every`, `all`, `nested`, `clear with`, `variant`
and reducers on the composite-keyed read model, a list query with a `by` block, a
non-optional single-instance return, and command reads (PLAY0271, or PLAY0781
reason `by-parts` under `command reads protected`) or trigger reads that produce directly.
Requires Stage 4.137.0 for v27 admission (older Stage refuses it with STAGE-ESM-016): the semantic Host runs composite-keyed read models end to end, and the specification executor refuses specifications over them as unsupported. The Cratis renderer refuses a read model with several key parts or a projection keyed by parts with STAGE-ESM-048 and a single-instance query by parts with STAGE-ESM-047 (Chronicle#4265); composite-keyed read models get no conventional `Get<ReadModel>ById` query. This is independent of standalone binding and reference execution. Read read-surface
[named keys](../../cratis-screenplay-read-surface/references/named-keys.md) for the
complete example.

## Uuid equality in command conditions (ESM v28)

Requires Screenplay 4.205.0 (ESM v28; `screenplay --version`); older binders refuse
Uuid comparisons with PLAY0268. `==` and `!=` bind in `require` and `produces when`
between required scalar Uuid command properties, Uuid concepts and identities,
and canonical Uuid literals; protected read paths bind in `require` only. When both
sides are concepts or identities they must be the same concept; a bare Uuid compares with
any Uuid concept. Refused with PLAY0268: ordering, optional operands, different concepts,
Uuid validation rules, Uuid concept rules, read paths in `produces when`, and text,
`$context` or reaction `where` operands. A literal that is not a lowercase hyphenated
Uuid reports PLAY0273. Stage refuses v28 with STAGE-ESM-016, independently of standalone
binding and reference execution. Read command-surface
[Uuid equality](../../cratis-screenplay-command-surface/references/uuid-equality.md) for the
complete example.

## Code attachments (what binds, what never does)

Pending `handler implementation` beside unconditional `produces` binds as a described
command (v23). Its payload-only body returns Unsupported ImplementationBody when reached;
denial/validation specs still run. Read [language admission](language-admission.md) for
v22–v28 and [described handlers](../../cratis-screenplay-command-surface/references/described-handlers.md)
for the result contract. Attached handlers remain unbound.

Inline fences (`csharp`, `typescript`, `react`, `html`, `sql`) and `file <path>` attach
code to an owner. Two kinds block binding (V3 blocked, no specification runs):
the attached command `handler` (PLAY0800) and the `file` constraint (PLAY0268, see below).
A query `performer` does not block: it is an opaque read, reported as information
`PLAY0270` and left out of the executable model; a specification that reads it is left
out, and every other specification still runs with `screenplay test`.
Requires Screenplay 4.220.1 (`screenplay --version`); older versions refuse a performer
query with PLAY0268 and leave the whole model unbound. Everything else binds as an **opaque requirement** that needs a target to
execute: reducer transitions, rule predicates, command and concept code validation, policy
predicates and reaction effects. Reaction effects bind only when the trigger has no
`reads`; a trigger with `reads` plus a `file` or inline effect fails binding. Reaction
bodies still leave their owner non-executable.

- A `file` constraint **never binds**: PLAY0268 "file implementation is not admitted by
  the executable model". Chronicle file constraints
  can only declare uniqueness: declare it with `unique ...`, and put other rules in command
  validation or a `require` condition.

- A file/fenced command `handler` refuses PLAY0800, wrapped or direct: a permanent
  requirement for a constrained implementation attachment, not future ESM admission.
  Gap-fill the slice in code; hosts read the required typed `arguments.subject` for
  the command rather than matching message text.
  Requires Screenplay 4.210.0 (`screenplay --version`); older binders use PLAY0268.
  A pending intent instead selects v23 with unconditional productions (PLAY0695 information).
  Missing productions use PLAY0696, conditional ones PLAY0697, duplicate handlers PLAY0698.
  Hints are not executable code, locks or confirmation.
- A command property named rule also accepts an `implementation` block (ordered
  `hint` lines, at most one `file` or tagged fence). With a source attached it binds like a
  direct source; hints-only or empty is pending intent: valid source, PLAY0268 at binding.
  The block is rejected on concept rules, built-in property rules and whole-command
  `require`/`validate` bodies.
- A reached described body is Unsupported, not a passing specification; Stage refuses
  it with STAGE-IMPL-002, even in Stub mode. Deliver its body as authorized gap-fill.
- Stage admits only pure reducer bodies (Roslyn allowlist, STAGE-ESM-019 and 022);
  code validation is refused (STAGE-ESM-005). Opaque pure policies render.
- The reference runner returns Unsupported for the opaque bodies that do bind (rule and
  policy predicates, code validation, reducer transitions); a specification that needs one
  never passes. Attached handlers and file constraints (and reaction effects on a trigger with
  `reads`) are different: they leave the whole model unbound, so no specification runs at
  all.

## Refused (code, reason): fix, or record as a mode gap

| Construct | Code | Note |
| --- | --- | --- |
| `Automation` or `Translate` slice, `reaction`, `capture`, `trigger`; `given clock`, `when clock`, `when trigger`, `when capture` | PLAY0268 | **older bundled compilers only**; current compilers bind them (ESM v6) |
| Unquoted `import Other.Contract` | PLAY0268 | external contract import is not bound |
| `pii` or `secret` on a concept | PLAY0268 | Older compilers refuse it outright. Current compilers admit v13 except enum/identity compliance and events mixing personal or explicitly subject-scoped secret values with explicit subject marks; see `compliance.md` in command-surface references |
| `query` with a `by` block over a list, or `by ... from` context-derived arguments | PLAY0268 | a `by` block binds only on an optional single-instance query (v27). An optional scalar snapshot lookup, an unfiltered list (`RM[]`) and a list narrowed by one caller-supplied `by` key bind, in their `observable` form too |
| Context `from`, single-result filters, `scoped to` | PLAY0268 | Typed query context, filtering of single results and opaque scope remain unadmitted; a `performer` query is not refused (PLAY0270 information, left out of the executable model); each refusal names its reason. Caller-supplied collection equality filters bind as v14. `observable` goes in the return position (`=> observable RM[]`); as a body line it is PLAY0048 |
| `contains` or `starts with` in conditions only | PLAY0268 | conditions admit scalar equality (Uuid from v28) and numeric ordering, plus same-primitive command date comparisons from v26; property/concept text rules bind as v24 |
| Date comparisons with mixed primitives, `$context` (including occurred), `today`, protected read paths, optional condition operands, concept rules or reaction `where` | PLAY0268 | v26 admits only command conditions and command/property literal validation rules |
| Rule on a nested path (`lines.qty all > 0`) | PLAY0268 | put the rule on the nested value's concept |
| Read paths without protected opt-in, or in `produces when` | PLAY0268 | v25 admits single-level paths only in `require` and produced-event mappings; a Uuid read path compared with `==`/`!=` follows the same positions |
| Uuid comparison with `<`, `>`, different concepts, optional operands, a validation rule or a concept rule | PLAY0268 | v28 admits `==`/`!=` in command conditions only, between the same concept or a bare Uuid and a concept |
| Legacy `reads <View> ...` on a command | PLAY0271 (error) | `command reads protected` opts eligible views into v25; see command-surface protected reads |
| `concurrency` block on a command | PLAY0271 (error) | "concurrency metadata keeps its legacy meaning and cannot bind to ESM v1" |
| File/inline `handler` (see above) | PLAY0800 | permanent attachment requirement: gap-fill in code; pending described handlers instead bind v23, with body execution/rendering separate |
| `$context.tenant`, roles, claims, causation | PLAY0268 | no portable scalar counterpart |
| Arithmetic or raw expression in a `produces` mapping | PLAY0268 | |
| Template `` `${x}` `` or `$causedBy` in a projection | PLAY0268 | |
| More than one `every`/`all` on one projection level | PLAY0268 | Chronicle keeps only the last |
| `$eventContext.<path>` other than `eventSourceId` | unsupported specs | binds, but execution refuses: "An event-context value other than the event source identity needs occurrence context that ESM v1 facts do not carry". `$context.occurred` in command `produces` is different and executes when the scenario has `given clock` |
| Projection-level `remove via join` | unsupported specs | binds, but `UnsupportedProjectionBlock` marks only the specifications that reach the projection unsupported; the others run and `planIssues` lists the deferred issue |
| `all` beside removals, children or nested on one level | unsupported specs | binds; `UnsupportedProjectionBlock` (scoped per specification, like the rows above) |
| `join`, `children` or `remove via join` inside `nested` | unsupported specs | binds; `UnsupportedProjectionBlock` (scoped per specification, like the rows above) |
| `$eventContext.occurred.Week` | PLAY0273 | derived value |
| Projection `parent p` where `p` is not on the child event | PLAY0273 | "Event property not found" |
| `generated` or `returns` on an older compiler | PLAY0268 | admitted from ESM v7, see above |
| Operations or systems, `numbers exact` source mode | PLAY0268 | not admitted by any supported ESM version; one such construct leaves the **whole** application without an executable model. Event sources/streams/routes instead bind as ESM v8 on the current compiler (`sources-and-streams.md`) |
| Spec of a gated command or query without `given caller` | PLAY0389 | module and feature gates count |
| `given` or `then readmodel` missing key parts | PLAY0351 | explicit `key` marks replace inference; composite fixtures need every part (v27) |
| When-less spec asserting events or errors | PLAY0352 | |
| `null` in command or event values | PLAY0350 | Chronicle semantics, not only a binder limit: an optional fact is a separate event (nullable event properties are CHR0012) |
| `for` with no unambiguous destination type | PLAY0273 | |

## Not refused, but do not rely on it

- Plain `produces` without `for` binds (PLAY0478, information) but means allocation in the
  ESM and "the identifier" in rendered Arc code. Always write `for` on a plain `produces <Name>`; inline `produces event` omits it.
- A projection-level (root) `remove via join` binds, but the reference execution plan refuses it (`UnsupportedProjectionBlock`): only specifications whose events reach that projection, or that supply, read, query or assert its read model, report unsupported, and the others run. Chronicle wires it as a child pull. See the unsupported-specs rows above.
  Requires Screenplay 4.213.2 (`screenplay --version`); earlier compilers refuse the whole plan, so no specification runs.
- V1 reports absent projection/children/nested targets
  with PLAY0514 and unknown removal/capture events with PLAY0166. An unmapped
  declared field can be found by opt-in `--check field-origins` (PLAY0534);
  unknown or opaque coverage is skipped, so still check lineage by hand.

## Never do this to reach V3

Do not remove `pii`, `secret`, `authorize` or policies, list queries, automations or
rules a domain needs just to make binding pass. Report `V3 blocked: <codes>` and record the
slice in the gap list. A `pii` or `secret` marker selects v13. Stage renders concept
protection; protected identities/enums still refuse `STAGE-ESM-040`. Either attribute on an identifier or typed destination is a source error
PLAY0515 (including stream-id parts and route mapping
sources). Chronicle also rejects `[PII]`/`[Encrypted]` on event-source identifiers
(CHR0034/CHR0052). Keep surrogate identity and protected payload separate.
