<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/executable-subset.md -->
# Executable subset (what binds: V2 and V3)

## Contents

- Caller and compliance boundary (4.142.0)
- Admitted on both compilers
- Equality query filters (ESM v14, since 4.143.0)
- Admitted from Screenplay 4.61 (ESM v6): standalone tool and cratis 3.28.2 or later
- Generated values and responses (ESM v7/v15): current compiler and renderer boundaries
- Released public boundaries and observer routes
- Code attachments (what binds, what never does)
- Refused (code, reason): fix, or record as a mode gap
- Not refused, but do not rely on it
- Never do this to reach V3

Current pins: `versions.md`. Detailed
4.66.0/4.68.0 binder probes below retain their historical labels. Current source
admits routes as ESM v8, public/direction constructs as v9, identity/overrides/filters
as v10, qualifying non-route Double literals as v11 (exact lowering of Double-mode literals)
and non-positional enum numbers as v12; concept compliance as v13; caller-supplied
collection equality filters as v14 since 4.143.0; concept generation strategies/freshness
as v15 since 4.147.0. Operations, the `numbers exact` **source mode** (PLAY0268) and
reaction refusals/redelivery remain unadmitted. Use the actual
binder before promoting an older probe to a current capability claim.

What the binder admits, and what it refuses with which code. The answer depends on the
compiler version: the standalone tool (4.149.0) binds through v15; the compiler bundled in cratis
3.57.0 (4.147.1) and its renderer admit through v15; older bundles (4.66.0 in cratis 3.28.x, admitting v1 to v6; 4.60.1 before 3.28.2, v1 to v5)
are history.
Tool facts and the exact v6 messages: `versions.md`. Always name
the tool with a V2 or V3 result. When tool and documentation disagree, the tool decides
current capability; the documentation and the domain decide intended correctness. Record
the gap, never strip the model to pass.

Binding is observed through MCP (`read-workspace view=executable-diagnostics`), a render, or
`cratis screenplay validate --executable <root>` (bundled compiler; binding errors only, writes
nothing; exit 5 when it does not bind). `screenplay <folder>` and plain `cratis screenplay validate`
never bind (`verdicts.md`).

Complete examples that bind live in `executable-example.md` (both compilers),
`pdl-example.md` (projections, both compilers) and `automation-translate-example.md`
(ESM v6; standalone tool and cratis 3.28.2 or later) and `generated-responses-example.md`
(ESM v7; standalone tool 4.68.0 or later, and the cratis 3.57.0 bundle).

## Caller and compliance boundary (4.142.0)

`$identity.id`/`.name`/`.userName` are aliases of the admitted
`$context.identity` produces-mapping values. Authentication status, roles and
claims still refuse there (PLAY0268); the older root is not deprecated.
A top-level `identity` block alone is authoring metadata and does not block
readiness. Executable reads of declared `$identity.<detail>` do (PLAY0268);
`$context.identity` remains built-ins only. Purposes and event `subject` marks
are metadata (PLAY0270), not source resolution, retention or subject propagation.
Bare `pii`/`personal` and `secret` concepts, scopes and qualifiers bind as v13
on standalone 4.142.0; CLI 3.57.0 bundles Screenplay 4.147.1, which binds them too. Enum/identity
compliance and events mixing protected values with explicit subject marks remain
refused; see command-surface `references/compliance.md` for the precise boundary.
Legacy spellings compile with PLAY0565 information; duplicates warn PLAY0653.
Never remove protection. Reference execution proves neither encryption nor erasure.
Authority: v4.142.0 `SemanticModelBinder.Compliance.cs`, `concepts.md`, `events.md`;
caller and purpose guidance retains v4.127.0 `context.md`, `identity.md`, `purposes.md`.

## Admitted on both compilers

- `StateChange` and `StateView` slices.
- A command with one `identifier`; inline `produces event <Name>` (omit `for`) for a command-owned event, or
  plain `produces <Event>` with explicit `for <identifier>` for a shared event.
- `produces when` with `==`/`!=` on scalar text, enum, number or boolean, and ordering on
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
  durability defects (`versions.md`): never treat admission as proof of uniqueness.
- A `readmodel` with one explicit trailing `key`, or an unambiguous inferred key:
  single-instance queries in the owning slice agreeing on `by xId XId`; only if
  none supplies identity and an admitted list query exists does conventional
  `*Id` inference run (collection-key exclusions: read-surface `references/named-keys.md`).
  Queryless read models need an explicit `key`. Explicit single keys preserve
  equivalent inferred ESM bytes. Composite read-model keys, query `by` blocks and
  their fixtures remain PLAY0268 (#599), not the 4.129.0 authoring delivery's
  executable admission. Collections and observable delivery bind. Since 4.143.0,
  caller-supplied equality filters on collections also bind as ESM v14; context
  sources, single-result filters, scope, performer and composite `by` remain blocked.
- One projection per read model: `from`, `every`, `all` (one block per level), `join`,
  `children`, `nested`, `remove with`, `remove via join` (binds; blocks the execution plan at the projection level, see below), `clear with`, counters, `add`,
  `subtract`, `set`, `clear`, variants, and the identifier mapped from the projection's key (`xId = $eventSourceId` only for an event-source key).
- Specifications: `given caller`, `given <Event> for`, `when <Command>`, `when append
  <Event> for`, `then <Event> for`, `then error "msg"`, `then denied`, `then query ...
  arguments ... result`, `then no readmodel RM for "key"` (ESM v5), and when-less view
  specifications ending in `then query` or `then readmodel`.
- `seed`. Personas, screens and descriptions are PLAY0270/PLAY0269 information, not
  blocking.

## Equality query filters (ESM v14, since 4.143.0)

`filter <name> <Type> [optional]` binds on collections with no `from`, targeting a
same-named scalar primitive/concept/enum property of the same nominal type
(ignoring optionality). Distinct filter names must not repeat `by`. All supplied
filters AND with `by`; optional omission does not narrow, supplied null is refused.
`then query … arguments` and `when query` accept both `by` and filters; PLAY0468
reports unknown/repeated/missing required arguments. Collection results match by
read model/key regardless of row order. The standalone 4.149.0 pin admits this;
CLI 3.57.0's 4.147.1 compiler and Stage 4.89.1 admit it through v15, subject to
construct checks (snapshot lists/DateTime filters/parameter order still refuse). Complete compiled specs:
read-surface [equality filters](../../cratis-screenplay-read-surface/references/equality-filters.md).

## Admitted from Screenplay 4.61 (ESM v6): standalone tool and cratis 3.28.2 or later

- `slice Automation` with `reaction`: `when <Event>` (with values) then `produces` (no
  `for` means the trigger's event source) or `invokes <Command>`; `where`; clock `at ... on
  ...` with an explicit `for`. Without a declared actor, a gated invocation denies.
  On 4.125.0 reaction `runs as system role "<Role>"` supplies the actor and
  selects ESM v10; keep the gate. CLI 3.41.0's older compiler rejected this syntax;
  The pinned CLI bundle (`versions.md`) supports it, and Stage renders role/authenticated reaction identity; claim-reading/opaque gates retain STAGE-ESM-034.
  Standalone reference execution admits it.
- Application `trigger` declarations and `when trigger` specifications.
- `slice Translate` with `capture` (`key`, `map ... translate`, `append ... when x from "a"
  to "b"`), and `given capture` / `when capture` specifications.
- `given clock` and `when clock`. The clock is UTC and exact; an occurrence fires if it is
  due **after** the given clock and **at or before** the when clock, once each, in time
  order. `when clock` needs `given clock`. Limits: 10,000 occurrences per advance, 1,000
  facts per scenario. A scenario with given equal to when fires nothing.
- Command specifications that list reaction cascades (no false PLAY0285).
- In a `then` list after `when append`, only what followed the appended event.

## Generated values and responses (ESM v7/v15): current compiler and renderer boundaries

Facts read at `v4.68.0`: decision 0026 and `Documentation/screenplay/{commands,specifications,interoperability}.md`.
A model selects ESM v7 only when it uses a generated property, a response, a generated fixture or a `then returns`
expectation; every other model keeps its version, bytes and revision. The cratis CLI 3.28.3 (bundled 4.66.0)
reported `PLAY0268` for these constructs (history). The current CLI (3.57.0, bundled 4.147.1) binds them
and Stage 4.89.1 renders supported UUID/String strategies, v15 freshness, scalar/named responses,
fixtures and `then returns`. Unsupported generated types (`-028`) and result combinations
(`-038`) remain refused; see `renderable-subset.md` and command-surface
`references/generated-values-and-responses.md`. The v15 rules below are sourced at
Screenplay `v4.148.0:Documentation/screenplay/generated-values.md:26-60`.
Complete examples: `generated-responses-example.md`, `generation-strategies-example.md`.

- A generated property is command-only, required, scalar and a generatable concept backed by Uuid or String;
  bare primitives, optional and collection types are invalid. Uuid has implicit `uuid`; String needs a strategy.
  Explicit serialized generation selects v15, with strict fixture encoding and whole-source freshness for all
  generated destinations. `pattern`, `random`, `rule`, `sequence`, generated rules/secrets remain unadmitted.
  See command-surface `references/generated-values-and-responses.md` and `generation-strategies-example.md`. Modifier order is `Type optional generated identifier`.
- Generated values are not request inputs, form fields, invocation arguments or ordinary `when` values. Generation
  runs after authorization and validation and before productions, so a denial or a validation failure never
  reaches it.
- A policy (including inherited or composed ones and the implicit `subject` of a generated identifier), a property
  rule or a requirement that references a generated value reports `PLAY0273`; reference input properties only. A
  generated property whose concept declares any validation rule (declarative, named or code) reports `PLAY0268`.
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

## Released public boundaries and observer routes

Public/origin events, explicit Translate direction, event-target builders and
`source events` captures bind as ESM v9 from 4.117.0. Publication folds execute;
source-event captures and reached opaque event-target reducers return typed
Unsupported. Public/foreign imports still lack a local shape and refuse PLAY0268.
See automations-and-translations `references/public-events-and-direction.md`.
Production overrides and reaction/reducer filters bind and select released v10;
filters match routed source/stream names only. Read
[production routes and filters](production-routes-and-filters.md).
Qualifying retained non-route Double literals bind/reference-execute as v11 on
4.135.0, unlike `numbers exact`; see `versions.md`.

Non-positional enum member numbers bind as ESM v12 from 4.139.0; positional
pins preserve the earlier executable version/bytes. The optional canonical last
`numbers` array parallels `values`; specifications still use member names.
Consumers must admit v12 explicitly; CLI 3.57.0/Stage 4.89.1 do. Syntax:
command-surface `references/enum-numbers.md`; version contract: `versions.md`.

Concept `pii`/`personal` and `secret`, qualifiers and scopes bind as optional
`compliance` in ESM v13 from 4.141.0 (rechecked at 4.142.0). Reasons remain
report-only PLAY0270; reference execution proves neither encryption nor erasure.
Enum/identity concepts and an event combining personal or explicitly subject-scoped
secret values with an explicit `subject` mark refuse PLAY0268 at binding only.
CLI 3.57.0 bundles Screenplay 4.147.1 / Stage 4.89.1 and renders v13 concept
protection; protected identities/enums still refuse `STAGE-ESM-040`. Preserve protection, not a falsely green bind.

## Code attachments (what binds, what never does)

Inline fences (`csharp`, `typescript`, `react`, `html`, `sql`) and `file <path>` attach
code to an owner. Three kinds block binding (PLAY0268, V3 blocked, no specification runs):
the command `handler`, the `file` constraint (see below) and a query `performer`
("uses delivery, filtering, scope, or implementation behavior outside the first ESM v1
vertical"). Everything else binds as an **opaque requirement** that needs a target to
execute: reducer transitions, rule predicates, command and concept code validation, policy
predicates and reaction effects. Reaction effects bind only when the trigger has no
`reads`; a trigger with `reads` plus a `file` or inline effect fails binding. Reaction
bodies still leave their owner non-executable.

- A `file` constraint **never binds**: PLAY0268 "file implementation is not admitted by
  the executable model" (`SemanticModelBinder.Constraints.cs`). Chronicle file constraints
  can only declare uniqueness: declare it with `unique ...`, and put other rules in command
  validation or a `require` condition.

- A command `handler` **never binds**: PLAY0268 "Command '<n>' handler requires a
  constrained implementation attachment." This holds with a `file`, with a fence, and with
  `implementation` or `hint` (including a pending intent with no payload). On a handler,
  `implementation` and `hint` are authoring intent: no execution, lock or confirmation.
- From 4.65.0 a command property named rule also accepts an `implementation` block (ordered
  `hint` lines, at most one `file` or tagged fence). With a source attached it binds like a
  direct source; hints-only or empty is pending intent: valid source, PLAY0268 at binding.
  The block is rejected on concept rules, built-in property rules and whole-command
  `require`/`validate` bodies (`v4.66.0:Documentation/screenplay/commands.md`, `context.md`).
- A handler therefore makes the slice non-executable and non-renderable: it is gap-fill
  with the model as the contract.
- Stage admits only pure reducer bodies (Roslyn allowlist, STAGE-ESM-019 and 022);
  code validation is refused (STAGE-ESM-005). Opaque pure policies render at Stage 4.73.1 (4.24
  refused them with STAGE-ESM-015).
- The reference runner returns Unsupported for the opaque bodies that do bind (rule and
  policy predicates, code validation, reducer transitions); a specification that needs one
  never passes. Handlers, file constraints and query performers (and reaction effects on a trigger with
  `reads`) are different: they leave the whole model unbound, so no specification runs at
  all.

## Refused (code, reason): fix, or record as a mode gap

| Construct | Code | Note |
| --- | --- | --- |
| `Automation` or `Translate` slice, `reaction`, `capture`, `trigger`; `given clock`, `when clock`, `when trigger`, `when capture` | PLAY0268 | **cratis before 3.28.2 bundles only** (4.60.1); messages in `versions.md` |
| Unquoted `import Other.Contract` | PLAY0268 | external contract import is not bound |
| `pii` or `secret` on a concept | PLAY0268 | Historical blanket refusal on CLI 3.53.1's 4.137.0 bundle only. CLI 3.57.0 and standalone 4.142.0+ admit v13 except enum/identity compliance and events mixing personal or explicitly subject-scoped secret values with explicit subject marks; see `compliance.md` in command-surface references |
| `query` with a composite `by` block, or `by ... from` context-derived arguments | PLAY0268 | composite `by` blocks remain unadmitted (#599). An optional scalar snapshot lookup, an unfiltered list (`RM[]`) and a list narrowed by one caller-supplied `by` key bind, in their `observable` form too (since 4.105.0) |
| Context `from`, single-result filters, `scoped to`, `performer` queries | PLAY0268 | Typed query context, filtering of single results, opaque scope and query implementation remain unadmitted; each refusal names its reason. Caller-supplied collection equality filters bind as v14. `observable` goes in the return position (`=> observable RM[]`); as a body line it is PLAY0048 |
| `contains` or `starts with` in conditions | PLAY0268 | equality and numeric ordering only |
| Date or DateTime comparison, `today` | PLAY0268 | no runtime date value |
| Rule on a nested path (`lines.qty all > 0`) | PLAY0268 | put the rule on the nested value's concept |
| `require` or `produces when` over a read-model path | PLAY0268 | needs decision-consistent reads (Screenplay#129) |
| `reads <View> ...` on a command | PLAY0271 (error) | legacy, cannot imply decision consistency |
| `concurrency` block on a command | PLAY0271 (error) | "concurrency metadata keeps its legacy meaning and cannot bind to ESM v1" |
| `handler` (see above) | PLAY0268 | gap-fill |
| `$context.tenant`, roles, claims, causation | PLAY0268 | no portable scalar counterpart |
| Arithmetic or raw expression in a `produces` mapping | PLAY0268 | |
| Template `` `${x}` `` or `$causedBy` in a projection | PLAY0268 | |
| More than one `every`/`all` on one projection level | PLAY0268 | Chronicle keeps only the last |
| `$eventContext.<path>` other than `eventSourceId` | blocked plan | binds, but execution refuses: "An event-context value other than the event source identity needs occurrence context that ESM v1 facts do not carry". `$context.occurred` in command `produces` is different and executes when the scenario has `given clock` |
| Projection-level `remove via join` | blocked plan | binds, but `UnsupportedProjectionBlock` blocks the whole reference execution plan: no specification in the application runs |
| `all` beside removals, children or nested on one level | blocked plan | binds; `UnsupportedProjectionBlock` |
| `join`, `children` or `remove via join` inside `nested` | blocked plan | binds; `UnsupportedProjectionBlock` |
| `$eventContext.occurred.Week` | PLAY0273 | derived value |
| Projection `parent p` where `p` is not on the child event | PLAY0273 | "Event property not found" |
| `generated` or `returns` on a compiler before 4.68.0 (4.66.0 and 4.67.0 refuse; 4.66.0 is bundled in cratis 3.28.x) | PLAY0268 | admitted from standalone 4.68.0 (ESM v7), see above |
| Operations or systems, `numbers exact` source mode, reaction refusals/redelivery | PLAY0268 | not admitted by any supported ESM version; one such construct leaves the **whole** application without an executable model. Event sources/streams/routes instead bind as ESM v8 on the current compiler (`sources-and-streams.md`) |
| Spec of a gated command or query without `given caller` | PLAY0389 | module and feature gates count |
| `given` or `then readmodel` missing key parts | PLAY0351 | explicit `key` marks replace inference; composite fixtures need every part but remain unadmitted (#599) |
| When-less spec asserting events or errors | PLAY0352 | |
| `null` in command or event values | PLAY0350 | Chronicle semantics, not only a binder limit: an optional fact is a separate event (nullable event properties are CHR0012) |
| `for` with no unambiguous destination type | PLAY0273 | |

## Not refused, but do not rely on it

- Plain `produces` without `for` binds (PLAY0478, information) but means allocation in the
  ESM and "the identifier" in rendered Arc code. Always write `for` on a plain `produces <Name>`; inline `produces event` omits it.
- A projection-level (root) `remove via join` binds, but the reference execution plan refuses it (`UnsupportedProjectionBlock`), so no specification in the application runs; Chronicle wires it as a child pull. See the blocked-plan rows above.
- At Screenplay 4.125.0, V1 reports absent projection/children/nested targets
  with PLAY0514 and unknown removal/capture events with PLAY0166. An unmapped
  declared field can be found by opt-in `--check field-origins` (PLAY0534);
  unknown or opaque coverage is skipped, so still check lineage by hand.

## Never do this to reach V3

Do not remove `pii`, `secret`, `authorize` or policies, list queries, automations or
rules a domain needs just to make binding pass. Report `V3 blocked: <codes>` and record the
slice in the gap list. A `pii` or `secret` marker selects v13 on standalone
4.142.0 (since 4.141.0). CLI 3.57.0 bundles Screenplay 4.147.1 / Stage 4.89.1, which renders concept
protection; protected identities/enums still refuse `STAGE-ESM-040`. Either attribute on an identifier or typed destination is a source error
PLAY0515 on the current compiler (including stream-id parts and route mapping
sources). Chronicle also rejects `[PII]`/`[Encrypted]` on event-source identifiers
(CHR0034/CHR0052). Keep surrogate identity and protected payload separate.
