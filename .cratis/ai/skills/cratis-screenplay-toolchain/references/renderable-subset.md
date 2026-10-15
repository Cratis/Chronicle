<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/renderable-subset.md -->
# Renderable subset (V5): what `cratis render` admits

## Contents

- Current admission (Stage 4.89.1)
- Historical: what Stage 4.24 admitted
- Authoring consequences in renderable mode
- Ownership, publication and recovery
- Reading V5

Current compiler/CLI/Stage pins: `versions.md`. CLI 3.57.0 / Stage 4.89.1 admit through v15 to construct
checks, not blanket rendering; above v15 is CLI-RENDER-004 (direct Stage:
STAGE-ESM-016). Public/foreign facts and production overrides retain 032/033/036.
Reaction identity renders role/authenticated gates, not claim-reading/opaque gates (034).
Reaction and reducer filters render on the semantic path; legacy paths retain 034/035. v8 routed read-model/query companion replay now renders; reserved names and
unformattable fixtures remain STAGE-ESM-030 (Stage
`v4.89.1:Documentation/reference/event-source-routing.md:62-72`). Case tables and v8+ `long`
whole numbers apply to the current default. Previous CLI 3.46.1/Stage 4.52.0
admitted only through v7 and applied neither newer rule. The historical v7 canonical-corpus check got
STAGE-ESM-028/029 for generated values/responses
(`v3.41.0:Source/Cli.Specs/for_ScreenplayPlanning/when_planning_an_esm_v7_model.cs`).
The following detailed Stage 4.24.x results are **historical**, not reverified
4.89.1 claims. Non-positional enum numbers select v12 and now render through
the CLI bundle; concept compliance selects v13 and also renders. Run the bundled renderer before asserting current renderability.

Historical sources, all read at tags: Stage v4.24.1 `Source/Rendering.Cratis/Semantics/SemanticSurfaceLedger.cs`
(an exhaustive map of every ESM member to Rendered, Rejected(code) or Ignored),
`CratisArtifactRenderPlanner.cs`, `PureTransitionAdmission.cs`; cli v3.28.2
`Commands/Render/RenderSettings.cs` and `Documentation/reference/screenplay.md`. Re-run the
render itself before claiming renderability: this table is a reading of the source, not a
render result.

Renderable is a strict subset of executable (`executable-subset.md`). Order of events in
`cratis render`: the CLI binds the **whole** model with its **bundled Screenplay** (4.147.1 in cratis
3.57.0, so PLAY0268 and friends appear first, even with `--scope`), then Stage admits or rejects
members of the selected scope. (Historical, cratis 3.28.2: an ESM v6 model bound but Stage refused it
whole with `STAGE-ESM-016`.)

## Current admission (Stage 4.89.1)

Read at Stage `v4.89.1` (`Source/Rendering.Cratis/Semantics/SemanticVersionFeatures.cs:105`,
`SemanticVersionFeatureAdmission.cs:25-46`, `SemanticCratisAdmission.StateView.cs:139-199`,
`Documentation/reference/reaction-rendering.md:57-98`, `generated-values-rendering.md:62-146`)
and CLI `v3.57.0:Source/Cli/Commands/Render/RenderedSemanticVersions.cs:34-47`.
Source reading, not a render run. Stage admits ESM v1 to v15 by
version; each construct it does not render refuses with its own code before any artifact is planned.

| Area | Renders at 4.89.1 | Refused (code) |
| --- | --- | --- |
| Slice kinds | `StateChange`, `StateView`, supported event-triggered `Automation` with Chronicle reactors | Translate, captures, non-event triggers, opaque bodies, clock/trigger/capture specs (`-024`); claim-reading/opaque `runs as` gates (`-034`), mixed effects from one handler (`-037`, redelivery can duplicate events), unsupported conditions/destinations/mappings (`-039`). Keep refused reactions whole |
| Queries | optional keyed snapshot lookups; observable collections, keyed or unkeyed, with v14 equality filters | a snapshot list: `STAGE-ESM-010`; context/single-result filters, opaque `scoped to`/`performer` and composite `by`: PLAY0268. DateTime filters and required parameters after optional filters refuse `-010`; filter-name collisions refuse `-012`; validated scalar keys render as primitives and convert inside the query (an out-of-rule key finds no instance), nested validated keys still refuse `-010` |
| Authorization | role-only, authenticated, opaque pure policies, claim conditions with Uuid, Date, DateTime or text targets (numeric and Boolean claim matches render as always false; Stage 4.73.1 `IsSupportedClaimPath`), `not`/and/or; roles read through the identity's `RoleClaimType` | role-claim URIs, nested opaque policies, unresolved or non-portable policies, no single identifier, opaque-policy subjects that are not text, Uuid, Date or DateTime (`STAGE-ESM-015`; Stage 4.73.1 `IsTextualSubject`, a restriction separate from declarative claim targets) |
| Event sources, streams, routes (ESM v8) | rendered, including routed read-model/query companion replay | reserved stored names, unformattable routed fixtures (`STAGE-ESM-030`) |
| Numbers, case tables | whole numbers as `long` from ESM v8; case tables | out-of-`int` literals below v8 (`STAGE-ESM-031`) |
| Generated values and responses (v7/v15) | required scalar UUID/String strategies, Guarded v15 freshness, scalar/named responses, generation fixtures and `then returns` | unsupported generated types (`-028`); routed/generated-rule/legacy-allocation/unique-constraint/name-collision or unsupported response combinations, unpinned unordered/projected expectations (`-038`); no blanket `-029` refusal |
| Enum numbers / compliance | explicit enum numbers (v12); concept `[PII]` or scoped `[Encrypted]` + `[NotAudited]` (v13), property-level `[NotAudited]` for protected command inputs | protected identities/enums (`-040`); non-text secrets (`-041`, not `pii secret`); explicit subject lineage remains unadmitted |
| Specifications | command scenarios and supported single-hop reaction appends; Stage's `admission.md` | appends outside the single-hop subset and commands cascading into reactions (`-011`); `then no readmodel` (`-027`); an unauthenticated caller carrying roles or claims, `$context.occurred`-derived fixed values asserted (`-011`) |
| v9 and v10 | reaction/reducer `from` filters; role/authenticated `runs as` | public events and event-target projections (`-032`); foreign origins and event-source captures (`-033`); directed Translate (`-024`); claim-reading/opaque `runs as` gates (`-034`); legacy `from` (`-035`); per-production route overrides (`-036`) |
| Evolved events | initial revisions only | `STAGE-ESM-026` (Stage#204) |
| Descriptions, documentation | XML `<summary>`/`<remarks>` doc comments only (Stage#178 closed) | characters invalid in XML: `CLI-RENDER-005`; never behavior |

CLI 3.57.0 bundles Screenplay 4.147.1. That compiler still refuses `reads` and
`concurrency` (PLAY0271), command `handler`s (PLAY0268) at binding, before Stage
admission. It binds through v15; supported filters and strategies render.
Response-name authoring scopes are in standalone 4.148.0, not this bundle or Scene runtime. Rows below naming Stage 4.24
behavior are historical.

## Historical: what Stage 4.24 admitted

Models using `eventsource`, `stream` or command routes are not renderable: they fail
binding (`PLAY0268`) and Stage refuses their ESM version (`STAGE-ESM-016`); rendered
appends never carry event source type, event stream type or event stream id, so
Chronicle's defaults apply (Stage#177, #200, #201). See `sources-and-streams.md`.

Whole-model admission requires ESM schema v1 to v4 on Stage 4.24.2 (v1 to v3 on the 4.24.1 that cratis 3.28.2 bundled, where a v4 model was STAGE-ESM-016 and also failed the CLI pre-check CLI-RENDER-003; cratis 3.28.3 bundles 4.24.2); v5 and v6 are STAGE-ESM-016. On 4.24.2 a selected event above its initial revision, or any scope depending on it, is STAGE-ESM-026 (migrations are not rendered, Stage#204), and a historical typed-context reference is STAGE-ESM-025. The admitted vertical is concepts,
composite types, one command to its events, a one-instance projection, an optional keyed
snapshot query, declarative authorization (including query authorization) and modeled
specifications. **Automation and Translate slices are not rendered** (Stage#79, open): the
whole automation is hand-written (gap-fill).

| Area | Renders | Rejected (code) |
| --- | --- | --- |
| ESM version | schema v1 to v4 (4.24.2; initial-revision events only) | evolved events: STAGE-ESM-026 (4.24.2); historical references: STAGE-ESM-025; v5 `then no readmodel`, v6 constructs, v7 generated values and responses: STAGE-ESM-016 for the whole model (Stage 4.24.1 also ledgers the v6 members as `STAGE-ESM-024`; users still see STAGE-ESM-016) |
| Slice kinds | `StateChange`, `StateView` | `Automation`, `Translate`: STAGE-ESM-001 |
| Types | concepts over Uuid, String, Int, Decimal, Bool, Date, DateTime; composite types; collections; optional | unknown types: 002 and 003 |
| Validation | not empty, min, max, equal, not equal, comparisons, length, `all >`/`all >=`, matches, `$strings` keys (missing default keys: 018) | code rules and code validation: 005 |
| State change | exactly one command per `StateChange` (004); unconditional `produces` with property mappings; typed `for`; tags; requirements; portable authorization; `$context.occurred` | `produces when` (006); value or literal expressions (006); other `$context` and event-context values (013); `reads` (PLAY0271) |
| State view | one projection per read model (007); `from`, `join`, `children`, `nested`, `every`, removals; set, add, subtract, increment, decrement, clear | composite keys, `all` (FromAll), event-context values, join keys, literal mappings in `every`/`all`, derived event-property names (017); Many or ZeroOrOne affected-instance cardinality (009); root join removal and nested shapes are blocked by Chronicle#4125 and #4166 |
| Queries | only the ZeroOrOne snapshot lookup by the read-model identifier, plus authorization | lists, `One`, observable, live, filters, paging: 010 or PLAY0268 |
| Reducers | bodies that pass the pure-transition allowlist (Roslyn; 023 is information) | impure bodies: 019 to 022 |
| Constraints | unique property value, unique event occurrence; admitted even though Chronicle#4123 means index updates run after commit | intra-command multi-event change to one constraint; multi-claim in one command: 014 |
| Authorization | authenticated, role, claim and logical policies; caller fixtures; ownership claims against text-backed targets | opaque policy code, role-claim URIs, a claim matched against a Uuid-backed subject or path: 015 |
| Specifications | command-action scenarios: given events, `when <Command>`, `then` events (ordered or any order), `then readmodel`, `then query`, `then error` (naming a rendered constraint or message), `then denied`, `given caller`. Two narrow when-less routes: an unprotected single `then query` seeded by exactly one `given readmodel` whose key and values match (no given events, no caller), and a query-only denial (`given caller`, one `then query` with no results, `then denied`, a protected query, no role claims) | `then no readmodel`, `when append`, composite values, error codes not naming a rendered constraint: 011. Any other when-less specification (for example given events with `then query`) fails 011 |
| Compliance | none | any `pii` or `secret` already fails binding (PLAY0268) on the pinned CLI. The C# provider mapping (Stage v4.29.0, Stage#197) is personal data to `[PII]`, operational secrets to `[Encrypted]` + `[NotAudited]`, both to `[PII]` only |

At standalone 4.127.0, compliance uses bare `pii`/`secret` (legacy markers are
accepted with PLAY0565 information); either still refuses binding on that compiler.
Standalone 4.142.0 admits concept compliance as v13 (from 4.141.0) with narrower refusals
(command-surface `references/compliance.md`). CLI 3.57.0 bundles Screenplay
4.147.1 / Stage 4.89.1 and renders concept protection as v13. Identity blocks,
purposes and event `subject` are authoring metadata, not provider realization;
executing identity detail reads still refuses PLAY0268. Current source/stream
routes are admitted as v8, including composite ids, not the historical binding
refusal above; the pinned CLI (`versions.md`) reaches Stage's v8 route checks. Never split hand-joined
legacy ids or drop protected ids into routing to fit a renderer (PLAY0515).

## Authoring consequences in renderable mode

- `produces when` is STAGE-ESM-006: split conditional outcomes into separate commands, or
  record the slice as non-renderable (gap-fill) without distorting a design-mode model.
- Only supported single-hop reaction `when append` specifications render; no general when-less event-replay proofs. Prove a projection
  with a command-action scenario (given events, `when <Command>`, `then query` or `then
  readmodel`). The seeded when-less `then query` only checks a lookup over a given read-model
  state; it proves no projection (Stage `SemanticSpecificationAdmission.Callers.cs`).
- Use `$context.occurred` only; no `$context.identity.*` mappings.
- A list query binds (unfiltered, or narrowed by one `by` key); Stage 4.89.1 renders it only as an
  `observable` collection, including supported equality filters, and refuses a snapshot list with `STAGE-ESM-010`;
  context/single-result filters, opaque `scoped to` and `performer` block binding. Keep a list the domain needs and report
  the capability gap; marking it `observable` or keying it is a modeling decision for the user.
- A `handler` (with or without `implementation` or `hint`) never binds; the slice is
  gap-fill, never "customized into" rendering.
- Descriptions and documentation render only as XML doc comments (Stage#178 closed), never as
  behavior: a rule that exists only in prose is unenforced in rendered code.

## Ownership, publication and recovery

- Ownership is the destination's **`.cratis-render.json`** manifest (semantic revision,
  identities, path and SHA-256 per artifact) and the **`.cratis-render/`** control directory
  (`journal.json`, staging, backups). There are no in-file markers. Do not stage
  `.cratis-render/` in git.
- Recovery runs before planning. A result with `recovered: true` means stop and reconcile.
- `--force` replaces a *modified active managed* file only. It never overwrites unmanaged
  files and never deletes modified stale files.
- `--name` is **required** for plain source and sets the application identity (it also
  defaults the project name and root namespace). Attachments are read from the model root,
  so render from a dedicated model folder.
- `Customizations/` is reserved and unmanaged (STAGE-CRATIS-005 if a model would render into
  it). Seams: `Customizations/Program.cs` (partial `ConfigureServices` and
  `ConfigureApplication`), `Customizations/*.cs`, `Customizations/Dependencies.props`,
  `Customizations/styles.css`. The planner never reads it, and it never binds a modeled
  reaction or capture. A customization never makes a rejected model renderable.
- `cratis render` does **not** build, test or run the application.
- Stage 4.89.1's scaffold pins .NET 10 and Arc and Chronicle client/kernel releases
  and emits a `.frontend/` React/Vite app (Components and Scene releases pinned too); these are not the latest
  releases (`cratis-stage-rendering-and-sandbox` `references/rendered-application.md`).
  `CratisEmbeddedScreenplayEnabled=false`; `Directory.Packages.props` is managed,
  never edit it. `[ProtectedDecision]` / `DecisionRead<T>` are available to gap-fill,
  not Stage-emitted enforcement; projection/key admission and unit-of-work enrollment
  remain required. Source checks: Stage skill `references/rendered-application.md`.

## Reading V5

V5 is four separate results (admission, publication, build, tests), plus runtime when it
was exercised; formats and the Debug-only spec classes are in `verdicts.md`. Three cases
stay distinct: admission failure (nothing rendered); an existing generated base plus
separately authorized hand-written gap-fill; and a fully hand-written delivery. Never claim a
whole-application V5 from a subset (`cratis render --scope` renders an admitted part; state the scope).
Drift: `cratis render ... --check` (CLI 3.44.0+; exit 6 means pending changes), or compare successive `.cratis-render.json`
`semanticRevision` values from renders with the same `--name` and inputs (see `versions.md`,
"Revisions and descriptions"); not the MCP `modelRevision`, not `open-workspace.revision`.

Workflow, gap-fill boundaries and recovery: `cratis-screenplay-render-and-gap-fill` and
`cratis-stage-rendering-and-sandbox`. The `cratis/stage` sandbox and the
`cratis/stage-specrunner` job are still shipped.
