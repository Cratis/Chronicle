<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/language-reference.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Screenplay language reference for authoring

## Contents

- Accepted constructs
- Caller, subject and stream nodes
- Container dependencies and exact numbers
- Complete model
- Source validity is not execution
- Not in the language yet
- Editor support

The `.play` source is plain UTF-8 text. Nesting is indentation-based, without
braces or terminators. A construct owns the content indented beneath it. `//`
starts a comment; escape reserved words inside a block with a leading `@`.
The grammar in `Documentation/screenplay/grammar.md` of the Screenplay
repository is the full list; this page is the authoring summary.

## Accepted constructs

**Top level:** `domain`, `import`, `concept`, `type`, `policy`, `persona`,
`authentication`, `identity`, `purpose`, `trigger`, `theme`, `layout`, `ui profile`, `behavior`,
`module`, `seed`, authoring-only `system`, and admitted `eventsource` with its `stream`s (ESM v8).

**Inside a module:** `description`, `authorize`, `import "<glob>"`,
`screen`/`dialog` templates, `form`, `contribute`, `on`/`uses` behavior
attachments, `feature`.

**Inside a feature:** `description`, `authorize`, `import "<glob>"`, nested
`feature`, `slice`, `contribute`, `on`/`uses` behavior attachments.

**Files (v4.48.0).** `import "<path or glob>"` - quoted - imports `.play` files
relative to the importing file; unquoted, `import` still names a contract from
another bounded context. At the top level it brings in whole documents. Inside a
`module` or `feature` it places each imported file there, and that file's top
level is the module's or feature's body, alongside any application-level
declarations it makes. A file matched by several imports is imported once, at the
deepest placement; conflicting placements (`PLAY0457`), cycles (`PLAY0458`) and
imports that match nothing (`PLAY0455`, `PLAY0456`) are reported where the import
is written. `screenplay <file>` compiles the application that file is the root
of; `screenplay <folder>` treats every file as a root and still places what the
imports place. The VS Code extension validates a workspace folder the same way,
as one application.

| Slice type | Meaning | Typical contents |
| --- | --- | --- |
| `StateChange` | Change the system | Command, events, validation, constraints |
| `StateView` | Read the system | Read model, projection, query, screen |
| `Automation` | React to something happening | Reaction |
| `Translate` | Translate external data into events | Capture |

Any slice can contain `description`, `file`, `event`, `command`, `query`,
`projection`, `capture`, `reaction`, `operation` (syntax-only), `screen`, `constraint`, `specification`,
`readmodel` and `reducer`. An unknown slice construct is only a warning
(`PLAY0029`) and its block is skipped; never accept a warning-free claim without
compiling the entire document set.

Concept primitives are `Uuid`, `String`, `Int`, `Decimal`, `Bool`, `Date` and
`DateTime`; `Enum` is a separate concept kind whose members are indented below
it. Concepts carry bare `pii` (`personal` alias), `secret`, reasons, personal-data
qualifiers, secret scopes and validation. Legacy markers report PLAY0565 information;
repeated header markers report PLAY0653 warning. Purposes declare the use/basis/retention
separately; see command-surface `references/compliance.md`.

Enum member `name = N` pins are unique int32 values, all members or none.
Unnumbered enums retain positions. Non-positional numbers bind as ESM v12 from
4.139.0; positional pins preserve older bytes/version. Fixtures and conditions
still name members. Syntax, stored-number safety and consumer admission:
command-surface `references/enum-numbers.md`.

Projections and captures use their dedicated sub-grammars. Inline code uses a
tagged fence (` ```csharp `); the older language-line form still parses with
warning `PLAY0397`. Query the MCP's `syntax-schema` instead of inventing JSON
members or translating names from memory.

## Caller, subject and stream nodes

At v4.127.0 the typed syntax includes `IdentityExpressionSyntax` (`$identity`),
`IdentitySyntax`, `IdentityDetailSyntax`, `ClaimIdentitySourceSyntax`,
`QueryIdentitySourceSyntax`, `CodeIdentitySourceSyntax`, `FileIdentitySourceSyntax`.
`ApplicationSyntax.Identity` carries the block; query schemas before editing JSON.
Built-in caller paths are aliases of `$context.identity`; declared detail reads
are authoring-only and refuse executable binding (PLAY0268), while the block alone
does not block readiness. Claim/query sources have exactly one source and no body.
See command-surface `references/context.md` for key/type/gate restrictions.

`PropertySyntax.isSubject` marks an event's required unprotected scalar subject,
omitted from structural JSON when false. It is lineage metadata (PLAY0270), not
runtime subject propagation or generated C# `[Subject]`. Read-model marks are
unsupported. `ReactionSyntax.RunsAs` / `ReactionIdentitySyntax` carry the system
actor (ESM v10); MCP `declaration-details` includes `runsAs`.

`EventStreamIdPartSyntax` and `streamIdParts` describe a composite stream-id schema;
route mappings cover all named parts. Current routes are admitted as v8, not an
old PLAY0268 gap. Never infer composite parts by splitting legacy scalar ids.

## Container dependencies and exact numbers

At Screenplay 4.125.0, modules and features accept `depends on <Name>`, one
per line. Bare targets resolve inside out (siblings, ancestor siblings, root
modules); qualified targets match an unambiguous trailing address. Merged files
accumulate each owner's declarations in file-path order; imports affect board
order, not accumulation. Dependencies are optional authoring metadata, not runtime
or implementation order. See `cratis-screenplay-slice-design` `references/slicing.md`
for coverage diagnostics and MCP `dependency-graph` `declarations` evidence.

A top-level `numbers exact` preamble opts each file into bounded decimal source
literals without Double rounding. All files/import barrels must agree on mode
(PLAY0508–PLAY0513 diagnose directive/range/mode errors). Exact mode is authorable,
not admitted by any supported ESM version (`PLAY0268`). Do not confuse this with
ESM v7 generated values and responses.

Typed roots (`ApplicationSyntax`, `ProjectionSyntax`, `CaptureSyntax`,
`SpecificationSyntax`) carry `sourceOptions: {"numericMode":"exact"}`. Missing
options default independently to Legacy, including nested roots; null/unknown
options and conflicting modes reject. Exact literal slots use
`{"literalType":"ExactNumber","value":"9007199254740993"}`: a canonical fixed-point
string, not a JSON number/JavaScript Number. Plain numbers, legacy envelopes and
noncanonical/out-of-range strings reject in Exact trees. An ExactNumber tag does
not opt a Legacy root in. Change mode as a whole-tree edit (leaves and all root
options), not just a flag; fragment printing keeps the owner's mode without a
nested preamble.

Sources: v4.125.0 `slices.md`, `folders.md`, `ast-authoring.md`, `grammar.md`.

## Complete model

This registration model uses an inline event and context identity rather than
copying the identifier into payload. It was compiled at main `fd18129` with zero
source diagnostics and recompiled with standalone 4.68.0 `--warnaserror`; neither
check runs its specifications. Inline lowering
and `optional` spelling do not themselves select a newer ESM version.

```screenplay
concept ProjectId : Uuid
concept ProjectName : String
module Projects
  feature Registration
    slice StateChange RegisterProject
      command RegisterProject
        projectId ProjectId identifier
        name ProjectName
        validate
          name not empty message "Project name is required"
        produces event ProjectRegistered
          name ProjectName = name
      specification RegisteringAProject
        when RegisterProject
          projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          name = "Screenplay"
        then ProjectRegistered
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          name = "Screenplay"
        then readmodel ProjectSummary
          projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          name = "Screenplay"
        then query ProjectById
          arguments
            projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          result
            projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            name = "Screenplay"
      specification RejectingAnEmptyProjectName
        when RegisterProject
          projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          name = ""
        then error "Project name is required"
    slice StateView ProjectLookup
      readmodel ProjectSummary
        projectId ProjectId
        name ProjectName
      query ProjectById => ProjectSummary optional
        by projectId ProjectId
      projection ProjectSummaryProjection => ProjectSummary
        from ProjectRegistered
          projectId = $eventSourceId
          name = name
```

## Source validity is not execution

The table and binding inventory below retain their historical 4.66.0/4.68.0
probe labels; do not use them as current tool admission evidence. At 4.125.0,
`screenplay test` and MCP `run-specifications` expose reference execution,
and sources/streams/routes bind as ESM v8. Operations and exact numeric mode
still refuse binding. Use the installed tool and the version table in
`cratis-screenplay-toolchain` for current verdicts, not an older negative probe.

A construct can be in one of four states. Say which one you checked.

| State | What establishes it | What it does not prove |
| --- | --- | --- |
| **Parsed** | `screenplay <folder> --warnaserror` or MCP authoring diagnostics: syntax plus the model-consistency checks (`PLAY0282`-`PLAY0294`, for example a specification event that no producer can emit, `PLAY0285`) | Binding, execution, or anything about attached code. It never binds, so it cannot report `PLAY0268`, and the tool never reads `file` attachments |
| **Bound** | Semantic binding to the executable semantic model (ESM): MCP `executableReady`, executable diagnostics | That the reference execution plan admits the model, or that any specification passes |
| **Reference-executed** | `screenplay test` or MCP `run-specifications` passes the selected specification | Target behavior; opaque code and external services never run here |
| **Target-executed** | Stage or a rendered application runs it with the implementations supplied | Nothing further in Screenplay |

Map the states onto the verdicts of `cratis-screenplay-modeling-lifecycle`:
Parsed is V1 (authorable); Bound is V2 (executable diagnostics read) and V3
(binding-ready, zero executable errors); Reference-executed is V4; Target-executed
is V5, reported as admission, publication, build and tests separately. A verdict
you did not run is "not run" with the reason, never implied by a lower one. MCP
`readiness.executableReady` can be true while executable diagnostics still hold
information entries (`PLAY0269`, `PLAY0270`).

The ESM version is selected by what a model uses, never by the author:

- **v1** is the default.
- **v2** is selected by typed event-source facts: `produces … for <identifier>`
  when the event does not repeat the identifier, `for` values in specifications,
  and `$context.occurred` or caller identity in `produces`.
- **v3** is selected by implementation attachments: a reducer whose rules all
  have bodies, a named `rule` with a body, a fenced `validate` block, or a
  `policy` implemented in code or a file. They bind as opaque requirements with
  capability `pure`. The reference runner returns `SemanticUnsupported` whenever
  it would need one, so a specification that depends on one never passes.
- **v4** represents complete event generations, with one contract identity and
  distinct revision property identities. Historical declarations are not migration
  implementations; a target without migration support must reject the contract.
- **v5** adds keyed read-model absence assertions (`then no readmodel`).
- **v6** (Screenplay 4.61 and later; v4.66.0 `Versions.cs`) admits reactions,
  captures, application triggers and the clock, hence `Automation` and
  `Translate` slices. The Cratis CLI 3.28.2 bundles Screenplay 4.66.0 and binds them;
  before 3.28.2 bundled 4.60.1 (ESM 5 at most), where these constructs failed
  binding with `PLAY0268`. Name the tool
  with every bound result; `cratis-screenplay-toolchain` (`references/versions.md`)
  holds the per-tool table.
- **v7** (Screenplay 4.68.0 and later; `Versions.cs`) admits generated command properties, `returns`
  responses, generated fixtures and `then returns` expectations, and is selected only by a model that
  uses one (decision 0026). The Cratis CLI 3.28.3 bundles 4.66.0 and reports `PLAY0268`; Stage 4.24.2
  refuses v7 with `STAGE-ESM-016` (tracked in Stage#201). Details:
  `cratis-screenplay-command-surface`.

Inline events lower to slice-owned standalone contracts and preserve equivalent
canonical bytes. Event `description`, `documentation` and rename-only `id` are
authoring metadata, not new ESM bytes or replacements for catalog identity.

Binding is not the last gate before the reference runner. The runner needs an
execution plan, and the plan is created only when every reachable capability is
admitted. One refused construct blocks the whole plan, so no specification in
the model runs, including specifications that never touch it.

What binds today, what the reference execution plan refuses, what is opaque, what is
authorable but not executable, and what blocks binding. The snapshot comes from the
v4.31.0 table, re-read against v4.66.0 and v4.68.0 documentation and probed through the MCP of
the standalone 4.66.0 tool for the rows marked "probed"; Cratis CLI 3.28.2 and 3.28.3 bundle
Screenplay 4.66.0 (before 3.28.2, Screenplay 4.60.1, differ where stated).

Read-model identity below is corrected to 4.135.0; equality query filters are
updated to standalone 4.149.0 (admitted in 4.143.0, ESM v14):
explicit trailing `key` wins; otherwise single-instance queries in the owning slice
must agree on `by`. Only with no single-instance identifier and an admitted list
query does conventional `*Id` inference run. Queryless views need explicit `key`.
See read-surface `references/named-keys.md` for the collection-key exclusion algorithm.

| Disposition | Constructs |
| --- | --- |
| Binds and runs in the reference runner | `StateChange`/`StateView` slices; `produces` including `when` conditions over command properties and literal tags; the portable validation rules, `matches email`, quoted `matches` patterns and `require` over command properties; declarative policies and `authorize` on modules, features, commands and keyed queries; `unique` constraints; projections as Chronicle lowers them, including variants, except the projection constructs in the next row; specifications, including `given caller`, `then denied` and `when append` |
| Binds, but the reference execution plan refuses it | A projection-level `remove via join`; `all` beside removals, `children` or `nested`; a `join`, `children` or `remove via join` inside `nested`; any event-context value other than the event source identity in a projection mapping or key, such as `$eventContext.occurred`, `$eventContext.sequenceNumber` or `$eventContext.causedBy.subject` (`$eventSourceId` and `$eventContext.eventSourceId` run) |
| Binds as ESM v6 on Screenplay 4.66.0, standalone and Cratis CLI 3.28.2 (probed); `PLAY0268` on Cratis CLI before 3.28.2 | `Automation` and `Translate` slices, reactions that produce or invoke, captures, top-level `trigger`, clock, trigger and capture specifications. The current standalone `screenplay test`/MCP reference route evaluates admitted v6 actions; the historical Stage 4.24.2 probe rendered none; Stage 4.89.1 renders supported event-triggered Automation, with remaining refusals in the Stage skill's `references/admission.md` |
| Reaction with trigger `reads` | Direct-producing (or code-bodied) reaction with `reads`: `PLAY0268` ("ESM v6 cannot protect that decision dependency", decision 0006), non-executable. Invokes-only reaction with `reads`: information only; the invoked command must declare and protect its own decision reads. Keep the `reads`; never remove them to obtain binding |
| Binds as opaque code (v3); reference runner reports unsupported | Bodied reducers, bodied named rules, fenced `validate` blocks, code or file policies. A pure reducer body is the only attachment Stage 4.24.0 admits |
| Information only, never blocks (probed) | `persona` (report-only: `PLAY0270`, the policies it lists must exist), `authentication`, `domain`, `seed`, descriptions and `file` provenance. A persona does not block binding and does not change behavior, so state persona scope in a description or a policy the model uses |
| Binds as ESM v7 on standalone Screenplay 4.68.0 (source and release notes; `PLAY0268` on the 4.66.0 in Cratis CLI 3.28.x, probed) | `generated` properties and `returns` responses, generated fixtures and `then returns`: bound and executed by the reference runner; a policy, rule or requirement that references a generated value is `PLAY0273`, and a generated property on a concept with validation rules is `PLAY0268`. Stage 4.89.1 renders supported UUID/String strategies, scalar/named responses and fixtures; unsupported types/result combinations remain `-028`/`-038` |
| Authorable, not executable (PLAY0268) | Systems/operations, the `numbers exact` source mode (PLAY0268; exact lowering of qualifying Double literals is the admitted v11) and refusal/redelivery remain unadmitted |
| Concept generation strategies (ESM v15, since 4.147.0) | Required scalar generatable Uuid/String concepts; concept-owned uuid/uuidv7/ulid/typeid/nanoid/bytes strategies and admitted modifiers. Strict fixture encoding; Guarded whole-source freshness rejects Concurrency with no append/retry. Rules, generated secrets, pattern/random/rule and sequence remain refused. CLI 3.57.0 / Stage 4.89.1 render supported strategies: command-surface `references/generated-values-and-responses.md` |
| Response-name UI continuations (4.148.0, no ESM change) | Form submit and execute-success scopes, own failure PLAY0685, nested shadowing and outer success in inner failure; field/parameter precedence. CLI's 4.147.1 lacks resolution; Scene runtime binding remains downstream. UI composition `references/response-continuations.md` |
| Collection equality filters (ESM v14, since 4.143.0) | Caller-supplied `filter name Type [optional]`, no `from`, matching a same-named scalar primitive/concept/enum read-model property with the same nominal type. AND with `by`; optional omission does not narrow, supplied null is refused. PLAY0468 covers unknown/repeated/missing required spec arguments; result rows compare by read-model/key regardless of order. Read read-surface `references/equality-filters.md` for the compiled example and renderer ceilings |
| Current routing and actor admission | Sources/streams and command/specification routes bind and reference-execute as v8; reaction `runs as system role` supplies the invocation actor as v10 on 4.125.0. Whole-workspace binding determines executable availability; CLI/Stage realization remains separate |
| Blocks binding (`PLAY0268`) | enum/identity compliance and events combining personal or explicitly subject-scoped secret values with explicit subject marks (4.141.0); historical CLI 3.53.1 refuses all `pii`/`secret` concepts; CLI 3.57.0 binds v13 and Stage 4.89.1 renders protection (historical probe: "compliance attributes require portable data-subject semantics"); command `handler` (never binds; a command cannot declare both `produces` and `handler`, `PLAY0035`), bare `rule <Name>`, `require` or conditions over read-model paths, dates and `today` in comparisons, `$context.tenant`/claims/roles/causation in `produces`, `$env` conditions, `file` constraints, unquoted `import` of an external contract, a read model with neither explicit `key` nor unambiguous query-based identity as described above (composite keys remain unadmitted), queries with context `from` sources (typed query context is unadmitted), single-result filters (collections only), `scoped to` (opaque scope), `performer` (opaque implementation) or composite `by` blocks (#599); optional keyed and collection queries, including caller-supplied equality filters and observable delivery, bind, `$causedBy` and templates in projections |
| Blocks binding (`PLAY0271`, legacy meaning) | Command `reads` and `concurrency` |
| Deferred (`PLAY0269`, information) | Screens, layouts, templates, forms, contributions, UI profiles, themes, behaviors and `on`/`uses` |
| Metadata only (`PLAY0270`, information) | `domain`, `seed`, `persona`, trigger and other descriptions, and `file` provenance on declarations |

The table is a snapshot, not a contract (rows without "probed" are carried from
v4.31.0 and v4.66.0 documentation): ask the binder (MCP `executable-diagnostics`
view) rather than extrapolating from it. Neither `screenplay` nor
`cratis screenplay validate` binds, so a clean validation says nothing about this table. Retain valid source that the
backend cannot represent rather than downgrading it to a stub. MCP `run-specifications` executes the admitted reference subset, not embedded
code or realization files. Stage owns application generation, rendering and
runtime admission.

## Not in the language yet

Decision 0006 shipped in Screenplay 4.31.0: a reaction trigger may declare
`reads <View> [as <alias>] [by <trigger value>]` (see
`cratis-screenplay-captures-and-reactions`). At v4.66.0 a direct-producing
reaction with `reads` fails binding (`PLAY0268`); an invokes-only reaction reports
the reads as information, and the invoked command must protect its own
dependencies. On Cratis CLI before 3.28.2 every reaction failed binding.

Do not infer availability from an accepted decision. Event generations and typed
diagnostic repairs now exist; see the event and MCP references rather than the
old blanket claim that decisions 0011–0014 are unimplemented. Generations describe
full historical shapes, not deployed-schema immutability or a working migrator.
Code attachment/source-map support likewise does not prove code round-trip
execution equivalence.

Operations remain authorable/unadmitted (PLAY0268). Named event sources/streams
and routes bind and reference-execute as v8 on 4.125.0; reaction identity as v10.
Use standalone `screenplay test` or MCP `run-specifications` for a reference
verdict, and independently check CLI/Stage admission. Never drop a contract to bind.
Generated values and `returns` are no longer in this group: they bind from 4.68.0 (ESM v7).
Protected reads and `derive`/`provide` (not admitted by any supported ESM version) have no documented
syntax at v4.68.0: do not teach or invent one. Affected-instance declarations and query paging/sorting/change-set delivery
remain unavailable here. Event `subject` is report-only authoring metadata at
4.127.0; public events/origin are admitted as ESM v9 (not provider realization).
Purpose declarations, references and processing records are authoring metadata,
not retention or lawful-processing enforcement.

`Type optional` is canonical for admitted type references; `Type?` remains
compatible with information `PLAY0479`. The modifier follows `[]` for an optional
collection. It changes no `IsOptional` meaning or ESM bytes. Keep the exceptional
one-shot result `query Q => observable?` when the type itself is named
`observable`: `observable optional` would be parsed as a live result of a type
named `optional`. Screen data, behavior parameters, concept bases and
`reads X optional` do not accept optionality.

## Editor support

Monaco uses `@cratis/screenplay-language`; VS Code uses `cratis.screenplay`.
Neither highlighting nor completion establishes compiler validity. Verify the
complete folder with the compiler after changes, and verify the owning downstream
runtime separately if execution is the goal.
