<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/renderable-subset.md -->
# Renderable subset (V5): what `cratis render` admits

## Contents

- Current admission
- Authoring consequences in renderable mode
- Ownership, publication and recovery
- Reading V5

Renderable is a strict subset of executable (`executable-subset.md`). Order of events in
`cratis render`: the CLI binds the **whole** model with its **bundled Screenplay** (so PLAY0268
and friends appear first, even with `--scope`), then Stage admits or rejects members of the
selected scope. Admission is by construct, not blanket: a model above the newest version the
CLI renders receives CLI-RENDER-004 (direct Stage: STAGE-ESM-016). Public/foreign facts and
production overrides retain 032/033/036. Reaction identity renders role/authenticated gates,
not claim-reading/opaque gates (034). Reaction and reducer filters render on the semantic path;
legacy paths retain 034/035. v8 routed read-model/query companion replay renders; reserved
names and unformattable fixtures remain STAGE-ESM-030. Case tables and v8+ `long` whole numbers
apply: below v8 an out-of-`int` literal is STAGE-ESM-031. Non-positional enum numbers (v12)
and concept compliance (v13) render.

## Current admission

The language is frozen at ESM v1–v28 ([the freeze decision](https://github.com/Cratis/Screenplay/blob/main/decisions/0092-the-language-is-frozen-at-esm-v28.md)): no new versions, constructs, keywords or grammar, except a construct for a real model that is blocked with no model-side alternative, accepted in its own record. Model with what v1–v28 offers; when Stage, `cratis render` or Studio does not support a construct yet, treat the typed refusal as a platform gap to report, since consumers are catching up to v28, not a reason to wait for new language.

Stage admits exact ESM v1–v27 pairs; v28 and mixed pairs refuse STAGE-ESM-016.
Requires Stage 4.139.0 for v22 admission; older Stage refuses v22 with -016,
while atomic mode remains -043. Chronological command dates select v26.
Requires Stage 4.141.0 to render Date/DateTime comparisons; older renderers refuse -046.
Read [rendered dates](../../cratis-stage-rendering-and-sandbox/references/date-comparisons.md)
when checking remaining -046 shapes and input-binding differences. Version admission
is separate from construct support.
Requires Stage 4.106.0 for v18 admission; older Stage refuses it with `STAGE-ESM-016`.
For generated-concept v20/v21 admission and construct prerequisites, read
[Stage admission](../../cratis-stage-rendering-and-sandbox/references/admission.md).
The CLI's bundled Stage can be older: read installed versions and the pins in
`product-sync.json`, not the standalone compiler's version. Each construct it
does not render refuses with its own code before any artifact is planned.

Requires Stage 4.96.0 for stored event names (v16), 4.101.0 for no-event assertions (v17), 4.102.0 for refusal branches (v19), and 4.103.0 for rendered redelivery; older renderers refuse the respective shapes.
Requires Cratis CLI 3.60.0 (`cratis --version`) for v16 rendering, 3.74.0 for v21–v26 and 3.75.0 for v27 admission (v28 and later: CLI-RENDER-004); a bundle below the model's version refuses it with CLI-RENDER-004, or with PLAY0268 when its bundled compiler predates the construct.

With an admitting renderer, all-Proven generated-concept rules allow unpinned
rendered specs; warning/info rules and semantic checksum validation are enforced,
and pattern/random strategies render. `generate rule` renders in a new enough bundle (see the Requires line below): the body
refuses -019 when it does not compile or warns, -022 on shadowing and -020 when the attachment is missing; the Host and legacy syntax engine keep -028.
Requires Stage 4.136.0 for `generate rule`, carried by Cratis CLI 3.75.0.
The independent executor still needs deterministic pins. Dated patterns use
receipt-time UTC; random ranges use `long` with inclusive endpoints.

This table is a reading of the renderer's admission, not a render result: run the bundled renderer before asserting renderability.

| Area | Renders | Refused (code) |
| --- | --- | --- |
| Slice kinds | `StateChange`, `StateView`, supported event-triggered `Automation` with Chronicle reactors | Translate, captures, non-event triggers, opaque bodies, clock/trigger/capture specs (`-024`); claim-reading/opaque `runs as` gates (`-034`), mixed effects from one handler (`-037`, redelivery can duplicate events), unsupported conditions/destinations/mappings (`-039`). Keep refused reactions whole |
| Queries | optional keyed snapshot lookups; snapshot or observable collections, keyed or unkeyed, with v14 equality filters | context/single-result filters, opaque `scoped to` and a list-query `by` block: PLAY0268; a `performer` query is an opaque read (PLAY0270) left out of the model ([version gate](executable-subset.md#code-attachments-what-binds-what-never-does)); v27 composite-key lookups by parts: STAGE-ESM-047, read models and projections keyed by parts: STAGE-ESM-048. DateTime filters and required parameters after optional filters refuse `-010`; filter-name collisions refuse `-012`; concept arguments retain Arc validation, including nested members/elements; nullable keys or optional concept filters with `not empty` refuse `-010` rather than changing null behavior |
| Authorization | role-only, authenticated, opaque pure policies, claim conditions with Uuid, Date, DateTime or text targets (numeric and Boolean claim matches render as always false), `not`/and/or; roles read through the identity's `RoleClaimType` | role-claim URIs, nested opaque policies, unresolved or non-portable policies, no single identifier, opaque-policy subjects that are not text, Uuid, Date or DateTime (`STAGE-ESM-015`, a restriction separate from declarative claim targets) |
| Event sources, streams, routes (ESM v8) | rendered, including routed read-model/query companion replay | reserved stored names, unformattable routed fixtures (`STAGE-ESM-030`) |
| Numbers, case tables | whole numbers as `long` from ESM v8; case tables | out-of-`int` literals below v8 (`STAGE-ESM-031`) |
| Generated values and responses (v7/v15/v20/v21) | required scalar UUID/String strategies and Int random, pattern/checksum, concept validators at all severities, all-Proven unpinned relational specs, Guarded v15 freshness, scalar/named responses and fixtures | unknown strategies and unsupported generated types (`-028`); `generate rule` bodies that do not compile (`-019`), shadow a type (`-022`) or lack an attachment (`-020`); unsupported response combinations, generated-property validation, name collisions, unpinned Unproven/Refuted rules or unordered/projected/collision-sensitive expectations (`-038`); invalid pins (`-011`); opaque concept rules (`-005`); unknown rule kinds and legacy checksum (`-042`) |
| Enum numbers / compliance | explicit enum numbers (v12); concept `[PII]` or scoped `[Encrypted]` + `[NotAudited]` (v13), property-level `[NotAudited]` for protected command inputs | protected identities/enums (`-040`); explicit subject lineage remains unadmitted |
| Specifications | command scenarios and supported single-hop reaction appends; Stage's `admission.md` | appends outside the single-hop subset and commands cascading into reactions (`-011`); `then no readmodel` (`-027`); an unauthenticated caller carrying roles or claims (`-011`); command `given clock` renders only whole milliseconds at or after 1970 |
| v9 and v10 | reaction/reducer `from` filters; role/authenticated `runs as` | public events and event-target projections (`-032`); foreign origins and event-source captures (`-033`); directed Translate (`-024`); claim-reading/opaque `runs as` gates (`-034`); legacy `from` (`-035`); per-production route overrides (`-036`) |
| Stored event names (v16) | `[EventType("storedName")]` retains stored identity while the class follows the model name | changed/unreadable destination event ids: `STAGE-EVENT-002`, CLI `CLI-RENDER-016` |
| No events / redelivery (v17) | otherwise admitted command no-event specs; one production-only reaction no-effect spec; located redelivery to one trigger with productions or one supported invocation | fan-out, deeper cascades, constraints, read/query expectations, redelivery with refusal branches, appends with no observer: `-011` |
| Refusal branches (v19) | ordered Arc `OnRefused`/`Acknowledge`, refusal values and supported single-hop append specs | same-event multi-trigger refusal groups, localized `$refusal.message`, ambiguous multiple-constraint `$refusal.constraint`: `-039`; specification limits: `-011` |
| Evolved events | initial revisions only (migrations are not rendered) | `STAGE-ESM-026` |
| Descriptions, documentation | XML `<summary>`/`<remarks>` doc comments only | characters invalid in XML: `CLI-RENDER-005`; never behavior |

The Cratis renderer renders protected command reads in a bundle that supports them (STAGE-ESM-044 for shapes it cannot preserve and in every earlier bundle), refuses described handlers
with STAGE-IMPL-002 (even Stub mode), and atomic mode in admitted pairs with
STAGE-ESM-043. Literal
text rules execute/render ordinal UTF-16 comparisons. Read [language admission](language-admission.md).
A CLI's older bundled compiler may fail first: legacy reads/concurrency PLAY0271,
attached handlers PLAY0800 (PLAY0268 before Screenplay 4.210.0). Protected reads bind v25. Non-Error protected requirements
refuse STAGE-ESM-045, including reaction invocations; this is not a rendering result.
Requires Stage 4.129.0 for semantic Host/executor protected reads (older engines refuse -044) and Stage 4.138.0 for the Cratis renderer to render them.
Response-name authoring scopes are not rendered and Scene runtime does not supply them.

Compliance mapping in the generated C#: personal data to `[PII]`, operational secrets to
`[Encrypted]` + `[NotAudited]`, both to `[PII]` only. Identity blocks, purposes and event
`subject` are authoring metadata, not provider realization; executing identity detail reads
still refuses PLAY0268. Source/stream routes are admitted as v8, including composite ids, and
the CLI reaches Stage's v8 route checks. Never split hand-joined legacy ids or drop protected
ids into routing to fit a renderer (PLAY0515).

## Authoring consequences in renderable mode

- Conditional productions render as guarded events; skipped productions make no
  freshness claim, and all skipped means accepted with no append. Multiple commands
  in one StateChange render. Expression/enum conditions retain STAGE-ESM-006.
  Requires Stage 4.98.0; older renderers refuse these shapes.
- Only supported single-hop reaction `when append` specifications render; no general when-less event-replay proofs. Prove a projection
  with a command-action scenario (given events, `when <Command>`, `then query` or `then
  readmodel`). The seeded when-less `then query` only checks a lookup over a given read-model
  state; it proves no projection.
- Use `$context.occurred` only; no `$context.identity.*` mappings.
- A list query binds (unfiltered, or narrowed by one `by` key); Stage renders snapshot and
  observable collections, including supported equality filters;
  Requires Stage 4.90.0 for snapshot lists and live `IReadModels.Observe` streams;
  older renderers refuse snapshot lists with STAGE-ESM-010.
  context/single-result filters, and opaque `scoped to` block binding; a `performer` query is left out of the executable model ([version gate](executable-subset.md#code-attachments-what-binds-what-never-does)). Keep a list the domain needs and report
  the capability gap; marking it `observable` or keying it is a modeling decision for the user.
- Attached file/inline handlers refuse PLAY0800 and require constrained implementation
  attachments: gap-fill in code, not by waiting for ESM admission.
  Requires Screenplay 4.210.0 (`screenplay --version`); older binders use PLAY0268.
  Pending handlers beside unconditional
  productions bind v23 but Stage refuses their bodies (STAGE-IMPL-002), even Stub mode;
  deliver as authorized gap-fill, never as implemented generated stubs.
- Descriptions and documentation render only as XML doc comments, never as
  behavior: a rule that exists only in prose is unenforced in rendered code.

## Ownership, publication and recovery

- Ownership is the destination's **`.cratis-render.json`** manifest (semantic revision,
  identities, path and SHA-256 per artifact) and the **`.cratis-render/`** control directory
  (`journal.json`, staging, backups). There are no in-file markers. Do not stage
  `.cratis-render/` in git.
- Recovery runs before planning. A result with `recovered: true` means stop and reconcile.
- `--force` replaces a *modified active managed* file only. It never overwrites unmanaged
  files and never deletes modified stale files.
- Persisted PATH roots use `.screenplay/identities.json` read-only, never temporary
  replacement identities. `--name` must match the saved name (CLI-RENDER-018);
  root-open failures are CLI-RENDER-017.
  Requires Cratis CLI 3.62.0 (`cratis --version`); older PATH rendering ignores persisted identities.
- `--name` is **required** for plain source and sets the application identity (it also
  defaults the project name and root namespace). Attachments are read from the model root,
  so render from a dedicated model folder.
- `Customizations/` is reserved and unmanaged (STAGE-CRATIS-005 if a model would render into
  it). Seams: `Customizations/Program.cs` (partial `ConfigureServices` and
  `ConfigureApplication`), `Customizations/*.cs`, `Customizations/Dependencies.props`,
  `Customizations/styles.css`. The planner never reads it, and it never binds a modeled
  reaction or capture. A customization never makes a rejected model renderable.
- `cratis render` does **not** build, test or run the application.
- Stage's scaffold pins .NET and the Arc and Chronicle client/kernel releases
  and emits a `.frontend/` React/Vite app (Components and Scene releases pinned too); these need not be the latest
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
Drift: `cratis render ... --check` (exit 6 means pending changes), or compare successive `.cratis-render.json`
`semanticRevision` values from renders with the same `--name` and inputs (see `verdicts.md`,
"Revisions and descriptions"); not the MCP `modelRevision`, not `open-workspace.revision`.

Workflow, gap-fill boundaries and recovery: `cratis-screenplay-render-and-gap-fill` and
`cratis-stage-rendering-and-sandbox`. The `cratis/stage` sandbox and the
`cratis/stage-specrunner` job are still shipped.

Requires Cratis CLI 3.44.0 or later (`cratis --version`) for `cratis render --check`.
