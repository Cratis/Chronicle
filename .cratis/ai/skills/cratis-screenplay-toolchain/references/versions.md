<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/versions.md -->
# Versions and capabilities by tool (the only version table)

## Contents

- Pin set
- Equality-filter rendering boundary
- Two compilers, one rule
- Commands and exit codes
- Model notes gate
- Admission boundaries
- Historical probes
- Revisions and descriptions
- Upgrade checklist

## Pin set

Use the released versions below. A source-checked capability is not a runtime
probe; older probes retain their original labels in the other references.

| Source | Pin | Evidence |
| --- | --- | --- |
| Screenplay language, standalone tool and MCP | **4.149.0** | Released tag `v4.149.0`, published NuGet tool and byte-identical Golden contract; concept generation MCP summary/entropy repairs, retaining ESM v15 and 4.148.0 response-name resolution |
| cratis CLI | **3.57.0** | Released tag `v3.57.0`; source-checked, not a new render/browser probe |
| Screenplay bundled by CLI | **4.147.1** | CLI `v3.57.0:Directory.Packages.props:8`; binds through v15; does not include 4.148.0 response-name continuation resolution or 4.149.0 generation summary/entropy repairs |
| Stage bundled by CLI | **4.89.1** | Same file, line 10; Stage `v4.89.1:Directory.Packages.props` pins Screenplay 4.147.1; rendering admits through v15 |
| Historical standalone Stage source baseline | **4.76.0** (Screenplay 4.139.0) | First v12 enum-number rendering; generated UUID-concept values and responses shipped in 4.74.0, v11 admission in 4.75.0. Those capabilities are now in the CLI bundle; standalone Stage beyond the bundled release is not this baseline |
| Screenplay.Generation / CritterStack / Prologue bundled by CLI | **0.18.0 / 0.24.0 / 2.0.0** | CLI `v3.57.0:Directory.Packages.props:5-7,28-33`. Latest released: Generation 0.18.1, CritterStack 0.26.0, Prologue 2.0.0 (no release after 2.0.0); the newer Generation and CritterStack releases are not in a cli |
| Cratis.Arc.Screenplay bundled by CLI | **22.54.0** | Same file; this is the generator package, not a blanket rendered-app dependency pin |
| Chronicle / Fundamentals bundled by CLI | **19.32.0 / 7.22.8** | Same file |
| Previously checked Arc / Chronicle / Fundamentals releases | **22.76.0 / 19.51.0 / 7.23.0** | Historical product reads, not latest-release claims. Arc 22.72.0+ supports events-and-commands reactor results; Stage still refuses mixed reactions because failed commands can cause duplicate events on redelivery |
| Stage 4.89.1 rendered backend and frontend | **Arc 22.83.0 / Chronicle 19.54.0**; `@cratis/components` 4.26.2, `@cratis/scene.*` 4.22.1, `@cratis/arc` 22.58.2, Fundamentals 7.22.8 | Stage `v4.89.1:Source/Rendering.Cratis/Scaffolding/CratisBackendApplicationScaffoldProfile.cs:59-71` and `CratisFrontendPackageSet.cs:208-229` (`Current`); not bumped by hand |
| Historical Stage 4.51.3 rendered backend | **Arc 22.50.5 / Chronicle 19.32.0** | Stage `v4.51.3:Source/Rendering.Cratis/Scaffolding/CratisBackendApplicationScaffoldProfile.cs`; client/testing/image match; scaffold disables embedded reverse extraction |

Scene's family id denotes the six scaffold packages listed in
`cratis-stage-rendering-and-sandbox/references/rendered-application.md`, not a
package named `@cratis/scene`. The Components skills verify the latest 4.26.4
surface; that does not upgrade Stage's managed scaffold pin.

The published `screenplay-contract.json` beside this reference is the machine-readable authority for PLAY codes, MCP tools and parameters, and standalone CLI commands/options. Its unchanged release bytes are pinned by `screenplay-contract.version` to the standalone version above. Before adding a claim, search this large JSON file by exact `code` in `diagnostics`, or by exact `name` in `mcpTools`/`cliCommands`, and inspect that object's retirement flag, parameters or options; do not load the full file into context. For example, search for `"code": "PLAY0029"` or `"name": "read-workspace"`; `screenplay-contract-exceptions.json` preserves explicitly reasoned historical statements, not current-release exceptions. Coverage gaps are mention inventories, not proof that a skill teaches a feature.

Check the actual executable before relying on this table: `screenplay --version`,
`cratis --version`, MCP `initialize` → `serverInfo.version`. An older executable
on PATH can shadow a newer install; inspect `which -a cratis`.

**Minimum for no-model MCP startup: Cratis CLI 3.46.1.** At `v3.46.1`,
`cratis screenplay mcp --project-root <project>` resolves a root without creating
it; startup, discovery and proposals leave no model folder, and the first
`apply` write creates it. `cratis ai install` creates no model folder unless a
root is configured. Older CLIs create the resolved root on server startup (`Screenplay/` when no `.play` files, `Source/` or `src/` exist). Released
source evidence: CLI
`Source/Cli.Specs/for_ScreenplayMcpInvocation/when_serving_a_project_without_a_model/and_the_client_writes_a_document.cs`
and `Source/Cli.Specs/for_AiMcpRegistration/when_installing/with_a_composed_profile.cs`
at tag `v3.46.1` (CLI#322 / CLI#311). This minimum does not re-label the compiler
or renderer probes below as results from the newer bundle.

## Equality-filter rendering boundary

Caller-supplied collection equality filters shipped in Screenplay **4.143.0**
(decision 0069), selecting ESM v14; the standalone **4.149.0** pin binds and
reference-tests them. No-filter models retain their version/bytes/revision.
CLI **3.57.0** bundles Screenplay **4.147.1** / Stage **4.89.1** and renders
collection equality filters, including optional omission and AND with `by`.
Snapshot lists still refuse `STAGE-ESM-010`. The semantic Host executes filters,
but the independent Stage specification executor refuses filtered query assertions
with `STAGE-ESM-010`; generated Debug specifications are a separate path.
Sources: CLI `v3.57.0:Directory.Packages.props:8-10`,
`Source/Cli/Commands/Render/RenderedSemanticVersions.cs:34-47`; Stage `v4.89.1`,
`Documentation/docker/index.md:143-153`, `Documentation/docker/spec-runner.md:35-43`.
These are source checks, not new render probes. Preserve equality selection rather than drop filters. Complete examples:
read-surface [equality filters](../../cratis-screenplay-read-surface/references/equality-filters.md).

## Two compilers, one rule

Standalone Screenplay 4.149.0 and CLI 3.57.0's 4.147.1 bundle are different releases.
Prefer standalone for the current language checks; CLI owns generation, Prologue
and rendering. Name the tool/version with every verdict. No verdict transfers
between differing compilers without checking the same inputs there.

A folder compiles as one application. A standalone file includes its imports,
not unimported siblings. Keep the application root as input for scoped checks.
Do not treat a clean fragment or scoped result as whole-application success.

MCP server launch: `screenplay mcp <model-root>` or
`cratis screenplay mcp <model-root>`. Discover `tools/list`, views and schemas
rather than counting tools. Connection, worktree switching and root-state
conflicts: `cratis-screenplay-model-authoring`. CLI 3.57.0's MCP 4.147.1 lacks
4.149.0's concept generation summary and PLAY0683/PLAY0684 repairs. Source:
Cratis/cli `v3.57.0:Directory.Packages.props:8,35-37`; Cratis/Screenplay
`v4.147.1:Source/DotNET/Screenplay.Mcp/McpDeclarationDetails.cs:153-160` and
`McpRepairCapabilities.cs:10-24`. The standalone generation fields and repairs
are source-checked at `v4.149.0:McpGenerationDetails.cs:13-83` under the same MCP
source folder; detailed evidence: model-authoring `references/mcp-tools.md`.

## Commands and exit codes

| Layer | Route | Verdict |
| --- | --- | --- |
| Source validation | `screenplay <root> --warnaserror --no-color` | 0 clean; 1 reported defects; 2 could not run/invalid arguments or input. `--scope` limits the reported set, not compilation |
| Completeness | Add `--check <name>[,<name>]` (repeatable) or `--check all` | Opt-in warnings; whole-source errors skip checks, not pass them |
| CLI source validation | `cratis screenplay validate <root> --warnings-as-errors -o json-compact` | Read producer exit and JSON errors/warnings/file count. Passing source-only probe on CLI 3.41.0: dependencies example, exit 0 |
| Binding | MCP workspace readiness and `executable-diagnostics` | V2 reads diagnostics; V3 requires binding with no blocking diagnostics |
| Reference specifications | `screenplay test <root> [--filter <address>] [--format text|json]`, or MCP `run-specifications` (`specification`, `scope`, `offset`, `limit`, `expectedSourceRevision`; `proposalId` from 4.145.0) | 0 all selected passed; 1 failed; 2 invalid arguments/input/selection or IO; 3 unsupported or unbound. `test` rejects `--no-color`; counts of zero do not prove coverage |
| Rendering | `cratis render` | Separate admission, publication, target build and target tests (V5) |

Source validation does not bind or run attached code. Reference execution is
in-memory and does not run opaque attachments or external services. CLI-derived
specification IDs need not match persisted MCP identities after renames; use MCP
for identity-stable selection. On Screenplay **4.145.0 and later**, run a retained
proposal read-only with `proposalId` after review and before apply. Without that
argument it runs the current workspace. An impossible `then` (PLAY0285)
unbinds the entire model and prevents every specification running.

## Model notes gate

**Standalone Screenplay 4.144.0** ships `--open-questions report|fail`.
**Cratis CLI 3.57.0 does not have this gate** on `cratis screenplay validate`.
Its bundled compiler is newer; that does not add the standalone tool's option to
the CLI adapter. Discover bundled MCP schemas separately. Historical:
**Cratis CLI 3.53.1 does not have this gate** and its older compiler predated notes tools.
Sources: Screenplay `v4.144.0:Source/DotNET/Tool/ModelOpenQuestions.cs:18-69,87-131`;
CLI `v3.57.0:Source/Cli/Commands/Screenplay/ValidateScreenplaySettings.cs:11-74`
and `Directory.Packages.props:8,35-37`.

```shell
screenplay <model-folder> --open-questions report --no-color
screenplay <model-folder> --scope Library.Loans.CheckIn --open-questions fail --no-color
```

`report` emits info `model-note.open-question` findings without failing for
questions. `fail` exits **1** when the selected set has an open question;
unreadable notes exit **2** when the flag is given. Compilation still runs with
its own verdict; question findings are MCP/CLI only, **no PLAY code**. With no
flag the sidecar is ignored, and `--warnaserror` does not gate questions. Tasks
and ordinary notes do not fail this gate.

Scope includes descendants and ancestor notes (application notes too). An
unscoped report includes orphaned open questions. Absent identity state reports
all open questions unscoped as orphaned with `identity-state-absent`, rather than
hiding them behind a scope. Notes never change ESM or V1–V5 verdicts. Read
model-authoring [model notes](../../cratis-screenplay-model-authoring/references/model-notes.md)
when recording, resolving, paging or reviewing orphan impact.

## Admission boundaries

Screenplay `v4.148.0:Source/DotNET/Screenplay/Semantics/Versions.cs:744-766` supports
schema pairs through v15. Admission selects by use:

- v7: generated values/responses and policy negation.
- v8: sources/streams and command/specification routes.
- **Released v9 (4.117.0):** public events/origins, Translate direction,
  event-target projections/reducers and events-source captures. Publication folds
  reference-execute; captures and reached opaque reducers return typed Unsupported.
  Public/foreign imports without local shapes remain PLAY0268. See
  automations-and-translations `references/public-events-and-direction.md`.
- **Released, frozen v10:** reaction `runs as system [role "<Role>"]` first
  shipped in 4.125.0; production route overrides and observer filters shipped in
  4.129.0. Marked released by #647 in 4.133.2. Decision 0066 records the shipped
  extension as a retroactive exception: consumers admit all three together.
  Checked actor scenario: captures-and-reactions `references/invocation-identity.md`;
  route/filter contract: [production routes and filters](production-routes-and-filters.md).
- **Published v11 in 4.135.0:** qualifying retained non-route Double-mode literals
  select v11 and execute with corrected decimal lowering (decision 0064). This
  includes whole numbers such as `9007199254740991` and qualifying fractions;
  unused/overridden examples and validation-only values do not select it.
  Already-lossless routes/route-feeding inputs do not select v11. This does not
  admit `numbers exact` source mode or arbitrary precision. At this tag,
  interoperability documentation still says "claimed, unreleased"; this corpus
  follows Versions.cs and decision 0066's publication boundary.
- **Published, frozen v12 in 4.139.0:** non-positional explicit enum numbers
  select v12, carrying optional canonical last `numbers` parallel to `values`.
  Positional pins retain earlier executable bytes/version/revision; unnumbered
  enums retain positional meaning. Names remain fixture/condition/specification
  values. Strict reading rejects arrays in older versions, without enum values,
  with mismatched length, duplicates, non-int32 entries or positional numbers
  (non-canonical). Consumers must admit v12 explicitly. Authoring shipped in
  4.137.0 (decision 0065); syntax/example: command-surface `references/enum-numbers.md`.
- **Published v13 in 4.141.0:** concept `pii`/`secret`, personal qualifiers and
  secret scopes bind as optional concept `compliance`. Unmarked models retain
  their exact bytes/version/revision. Reference execution outcomes stay unchanged;
  it proves no storage encryption or erasure. Enum/identity compliance and an
  event combining personal or explicitly subject-scoped secret values with an
  explicit `subject` mark refuse PLAY0268. See command-surface
  `references/compliance.md` and Screenplay decision 0068. CLI 3.57.0 and
  Stage 4.89.1 admit v13 and render protection; protected identities/enums
  remain `STAGE-ESM-040`.
- **Published v14 in 4.143.0:** caller-supplied equality filters on collection
  queries with same-named, type-matched scalar read-model properties; no `from`.
  Optional filters may be omitted, supplied null is refused, and `by` plus supplied
  filters combine with AND. Models without filters preserve bytes/revision.
  Screenplay `v4.144.0:decisions/0069-admit-equality-query-filters-as-esm-v14.md:43-88`;
  CLI 3.57.0 / Stage 4.89.1 now admit and render these filters.
- **Published v15 in 4.147.0:** concept `generate uuidv7`, `ulid`, `typeid`,
  `nanoid`, `bytes` and String-backed `uuid`, with admitted modifiers. Only a
  serialized `generation` member selects v15; unmodified Uuid `generate uuid`
  preserves earlier bytes. All generated destinations in a v15 invocation are
  Guarded against whole-source reuse, including implicit UUIDs; strict fixtures
  must match strategy encoding/version/variant. Reuse rejects with `Concurrency`,
  without retry or partial append. CLI 3.57.0 / Stage 4.89.1 render supported
  strategies with explicit atomic freshness scopes. Details and remaining refusals:
  command-surface `references/generated-values-and-responses.md`.

Decision 0066 freezes a version when any exposing NuGet/npm package is published,
including prereleases. Later byte-changing features take the next version at
admission; a byte-preserving extension under 0004 is the exception. Published
numbers never change or get reused. Enum member numbers are admitted as v12;
this does not admit flags or numeric values in name-based specifications.
High-entropy generation and freshness are admitted; `pattern`, `random`, `rule`,
generated-concept rules and compliance remain unadmitted. `sequence` reports PLAY0682. Operations,
exact source numeric mode and reaction refusals/redelivery remain PLAY0268.
Composite read-model keys/query blocks are also still PLAY0268 (#599), not
admitted by the 4.129.0 authoring delivery; a single explicit key preserves bytes.
Never delete these contracts to obtain a green bind.

CLI 3.57.0 `RenderedSemanticVersions.Newest` is **v15**
(`Source/Cli/Commands/Render/RenderedSemanticVersions.cs:34-47`). Stage 4.89.1
uses `EsmSchemaV15Support` (`Source/Rendering.Cratis/Semantics/SemanticVersionFeatures.cs:105`)
and construct checks in `SemanticVersionFeatureAdmission.cs:25-46`.
Through v15 is not blanket render support: public publication STAGE-ESM-032,
foreign events/event captures 033 and production overrides 036 remain refused.
Reaction `runs as` renders role-only/authenticated-only gates; claim-reading or
opaque gates still refuse 034. Reaction and reducer `from` filters render on the
semantic planner; legacy syntax paths retain 034/035. Non-text secrets refuse 041.
Single-hop reaction append specifications render; deeper cascades still refuse 011.
Source: Stage `v4.89.1:Documentation/reference/reaction-rendering.md:57-98`. ESM v8 routes reach their own checks.
Previous CLI 3.46.1/Stage 4.52.0 admitted only through v7 (CLI-RENDER-004 and
STAGE-ESM-016 respectively). Historical CLI 3.41.0/Screenplay 4.114.0 rejected
`runs as` at parsing (PLAY0137, exit 5), not a current parser claim. Each unsupported
construct needs a diagnostic, not silent omission or guessed package support.

The renderable-subset and Stage references retain older probes explicitly;
read them as tested snapshots, not as a claim that Stage 4.52.0 shares every
4.24.2 limitation. Query the actual bundled renderer before a V5 claim.

Stage case tables require 4.59.0; ESM v8 whole numbers as `long` and the
pre-v8 out-of-`int` refusal STAGE-ESM-031 require 4.61.0. Historical CLI 3.50.0
defaulted to Stage 4.67.0; current CLI 3.57.0 defaults to 4.89.1, so both rules apply. Previous CLI 3.46.1 bundled Screenplay
4.130.0 and Stage 4.52.0; neither rule applied there. `cratis run --tag 4.63.2`
selects a newer sandbox image only (`RunSettings.cs`), not a renderer package.
`cratis render` has a statically bundled target (`RenderSettings.cs`,
`CratisRenderTarget.cs`); a newer renderer needs a CLI release that pins it.
The Stage skill teaches the version-gated examples.

At 4.127.0, `$identity` aliases built-in caller paths; declared identity details
remain metadata and their executable reads report PLAY0268. Bare `pii`/`secret`
bind as v13 on standalone 4.142.0 and CLI 3.57.0's
4.147.1 bundle; `personal` aliases `pii`. Legacy markers are accepted with
PLAY0565 information, duplicates warn PLAY0653. Event `subject` and processing
purposes are report-only metadata (PLAY0270); purpose checks/reports do not prove
lawfulness or enforce retention. See command-surface context/compliance references.

Authoring features source-checked in standalone 4.127.0: `propose-source`,
`find-specification-obligations`, `find-modeling-smells`, specification descriptions,
container/command/read-model/reaction documentation, routed redelivery locators,
guarded item interactions, persona callers and named case tables. Discover schemas
in the older CLI bundle rather than assuming parity. Cases/persona callers expand
to ordinary ESM; metadata changes no executable bytes. Redelivery remains
unadmitted even when its route is valid. Details: model-authoring MCP references,
specifications, UI composition and `diagnostics.md`.

## Historical probes

- Screenplay 4.68.0 admitted v7; CLI 3.28.2/3.28.3 bundled 4.66.0 (v6 at most),
  Stage 4.24.1/4.24.2. Those old negative v7 probes do not describe CLI 3.41.0.
- CLI before 3.28.2 bundled Screenplay 4.60.1: false cascade PLAY0285, v6 refusal
  and dynamic-root initialization bug. The roots bug was fixed in 4.63.2.
- CLI 3.28.2 file validation ignored imports (cli#244, closed by PR #258): later CLI releases compile
  the file as an application root together with its imports. Folder mode stays the recommended check;
  validate the complete root.
- Older standalone empty-folder probes exited 0. Current validation still needs
  a nonzero file count, and specification execution needs an expected selection.

## Revisions and descriptions

`modelRevision` exists only when binding succeeds; it is canonical semantic
identity, not source identity. Source positions and report-only metadata can
change source/workspace revisions without changing it. Persisted catalogs
preserve assigned identities; do not reconstruct them from names.

A render manifest's `semanticRevision` is a different provenance value, made
with the render name and bundled compiler. Do not compare it directly to MCP
`modelRevision`. Compare successive renders with the same identity and inputs, or use
`cratis render ... --check` (CLI 3.44.0 or later). Descriptions and event documentation render as XML
doc comments, so editing them changes managed-file hashes (a `--check` exit 6) but not the executable
model revision.

## Upgrade checklist

1. Read installed versions and released dependency pins.
2. Check source, binder, reference runner and renderer admission separately.
3. Compile each changed example; execute specification examples with the
   reference route. Preserve explicit expected-diagnostic checks.
4. Re-probe affected capabilities before replacing a historical version label.
