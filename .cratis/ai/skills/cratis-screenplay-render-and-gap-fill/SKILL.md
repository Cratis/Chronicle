---
name: cratis-screenplay-render-and-gap-fill
description: Use when probing whether an accepted Screenplay model renders, delivering admitted scope or triaging a renderer gap. Not for changing the model, Stage internals or auditing hand-written slice conformance.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-render-and-gap-fill/SKILL.md -->

# Screenplay render and gap-fill

## Purpose

Act as a **capability probe and deterministic delivery verifier**, not as a code author.
`cratis render` compiles and binds the whole model, then admits the selected scope (the whole
application, or `--scope` modules, features and slices): if any construct in that scope is outside
what Stage admits, that run publishes nothing. This skill finds out what renders, publishes it,
proves it builds and that its generated specifications pass, and reports everything else honestly.
Behaviour the renderer rejects is delivered by `slice-implementer` with the `.play` model as
the contract, never smuggled into `Customizations/`.

Model-first: the model is the source of truth, Stage-managed output is derived, and hand-written
code exists only for infrastructure, adapters and scope Stage cannot render yet. The decision
rule is in `cratis-screenplay-modeling-lifecycle` ("Decide the level first"); it is not restated here.

## Verified product sources

Tool versions are covered in `cratis-screenplay-toolchain` `SKILL.md`; read it before selecting the compiler and renderer for delivery: the CLI bundles its own Screenplay and Stage. Read [references/product-sources.md](references/product-sources.md) when checking a render claim against the CLI, Stage or probe sources; the renderer facts are owned by `cratis-stage-rendering-and-sandbox`.

## When to use / when not

- Use: an accepted model (P6, bound to a source identity) must become running code; "would this
  render?"; a re-render after a model change; a drift check; a build failure in `Customizations/`.
- Not for: changing the model (return edit requests to the modeler session); choosing the mode
  (the user); Stage internals and the sandbox (`cratis-stage-rendering-and-sandbox`); judging
  hand-written code against its slice (`cratis-application-slice-conformance`).

## Language modes that block target delivery

Read [language admission](../cratis-screenplay-toolchain/references/language-admission.md)
for version selection, and [current Stage boundaries](../cratis-stage-rendering-and-sandbox/references/current-boundaries.md)
for realization. Bounded atomic batches, source-event Translate captures/publication and resolved
C# validation bodies now render. Described handlers still refuse even in Stub
mode; query-performer gaps are a separate contract, not completed behavior. Protected reads still need an admitted shape. Semantic Host/executor
support is not generated-app protection.
Preserve contracts; reference success is not target realization.

A file/inline command handler binds beside unconditional `produces` with a resolved
body (v23 result contract, Screenplay 4.250.0); without them it refuses PLAY0800 with a
required typed `arguments.subject`. 4.210.0 to 4.249.x refuse every attached handler
with PLAY0800; older binders use PLAY0268. Read [renderability gate](references/renderability-gate.md)
when classifying a handler or other binding refusal; never parse message text.

Inline bodies (`csharp`, `typescript`, `react`, `html`, `sql`, `java`) are opaque and reach the target
unchanged; each target decides which it realizes. The Cratis target refuses `java` bodies: gap-fill
that scope in code and record the refusal.
Requires Screenplay 4.244.0 (`screenplay --version`) for the `java` refusal.

## Interview phase
Ask only for missing inputs: accepted model/root, stable `--name`, destination,
non-renderable-scope decision and seams. Read [references/delivery-protocol.md](references/delivery-protocol.md)
when selecting defaults. Unattended, state probe-name/destination assumptions;
never assume the non-renderable-scope decision: record it as blocked.

## The three cases (decide first, say which one applies)

| Case | Situation | What happens |
| --- | --- | --- |
| **A. Admission failure** | the probe reports blocking diagnostics | nothing was rendered or published by this run. Report the codes; the user chooses the next step (see "Procedure" 3) |
| **B. Generated base plus authorized gap-fill** | an admitted render (whole, or a `--scope` of the admitted slices) exists and the user separately authorized hand-written scope, or a seam the model names needs an adapter | the generated output stays managed and untouched; hand-written code lives in `Customizations/` or a separate project (seam adapters by this skill, business gap-fill by `slice-implementer`), serves named model elements only, never claims render admission, and V5 is reported for the generated base **with its scope stated** |
| **C. Fully hand-written delivery** | the scope cannot render and no generated base is wanted | `slice-implementer` delivers it with the model as the contract (`references/gap-fill-handoff.md`); V5 is `not run: no render` |

Evolved events and dependent scope refuse `STAGE-ESM-026`; initial revisions render.
Gap-fill migrations per `cratis-chronicle-event-type-migration`, preserving the model contract.

Supported UUID/String generation, v15 freshness, responses, fixtures and
`then returns` render. Unsupported generated
types (`STAGE-ESM-028`) and result combinations (`-038`) still require case A or C:
preserve the model and its specifications. See Stage's `references/admission.md`.
Supported event-triggered Automation slices also render as managed Chronicle reactors;
bounded source-event captures and outbound Translate also render; refused reaction/Translate shapes remain gap-fill. A `given clock` fixture
in the semantic executor is not support for rendering clock triggers.
Requires Stage 4.141.0 to render v26 Date/DateTime command comparisons; older renderers
refuse `STAGE-ESM-046`. Read [rendered date comparisons](../cratis-stage-rendering-and-sandbox/references/date-comparisons.md)
when delivering dates for remaining refusals and input-binding differences.

Source/stream routes bind and reference-execute as v8; target admission is separate.
Requires Cratis CLI 3.75.0 for v27 admission; older bundles stop earlier.
The reviewed CLI admits semantic versions through v30; its bundled Stage then
applies matched-pair and construct gates. V32 needs a direct caller of the reviewed
Stage baseline; see [CLI bundle details](../cratis-screenplay-toolchain/references/language-admission.md#stage-admission-and-execution).
Read toolchain `references/renderable-subset.md` and
`references/sources-and-streams.md` when delivering routes, actor gates or filters.
Preserve refused public/foreign events, production overrides and opaque gates as A/C.

**Mixed effects (`STAGE-ESM-037`).** Keep the reaction whole: returned events append
before commands and redelivery can duplicate them. `[OnceOnly]` excludes replay,
not retries. Read Stage's [admission](../cratis-stage-rendering-and-sandbox/references/admission.md)
when gap-filling a mixed reaction; prove retry safety, never split or drop effects.

Read Stage's `references/admission.md` when checking v17/v19 or query validation;
query errors need v18 admission. Preserve rules and test rejection separately from absence.

Two rules hold in every case: a customization never makes a rejected model renderable, and
a whole-application V5 is never claimed from a subset. `--scope` renders an admitted part (the whole
model still compiles and binds) and its V5 states the scope.

## Procedure

1. **Preconditions.** P6 has committed the accepted `.play` files and `.screenplay/identities.json`
   when present; uncommitted model changes return to P6 before delivery. Record the source identity (`cratis-screenplay-modeling-lifecycle`
   `references/verdicts-and-modes.md` "Source identity"), the model root, `--name`,
   `--project-name`, `--root-namespace` and target `cratis`. Take V1 and V3 from
   `cratis-screenplay-toolchain`; report each with tool and version. V3 from a different
   compiler version does not prove the cratis-bundled binder admits the model (the bundle
   can be older than the standalone tool). `cratis screenplay validate --executable <root>` binds with
   the bundled compiler and writes nothing: use it as the bundled-binder check (toolchain `references/verdicts.md`).
2. **Probe.** `cratis render <model-root> --name <App> --destination <probe-dir> -o json`
   (exit 0 published, 5 refused with nothing published, 1 missing input); add `--check` to plan
   against the destination without writing (exit 6 changes pending) and `--scope <Module.Feature.Slice>`
   (repeatable) to render an admitted part. Classify every
   diagnostic with `references/renderability-gate.md`: model fix in the chosen mode,
   capability gap, permanent implementation-attachment requirement, tool skew or environment. Group by code and count; do not paste the list.
   Requires Stage 4.146.0 for collecting independent admission refusals in one plan;
   earlier renderers reveal later batches after earlier fixes. Profile/version failures
   still stop first. Classify all returned refusals before the next probe.
3. **When admission fails** (case A) present the options, the user decides:
   (a) keep design mode and record the gaps; (b) narrow to renderable mode, where the modeler
   changes the model for business reasons and **without** removing protection (`pii`,
   authorization, rules); (c) deliver the non-renderable scope by hand (case C), with a fallback
   record entry (`references/drift-and-fallback-record.md`); (d) render the admitted part with
   `--scope` and gap-fill the rest (case B).
4. **Publish.** The probe destination by default. Another destination needs the user's intent;
   `--force` needs approval naming its effect and destination (a request that already names them
   counts). Read the receipt: written, unchanged, removed, `recovered`. `recovered: true` means
   stop and reconcile. A refusal is a drift event, not an error to bypass.
5. **Build, then test**, each its own bounded call and only if the previous result passed. **Debug
   only**: generated specifications compile under `#if DEBUG`. Use the repository's runner
   with an explicit timeout (commands and reading the results: `references/delivery-protocol.md`).
   Map results per `.play` specification through the generated classes: expected, with class,
   passed, failed, no class.
6. **Seam adapters (Customizations only)** for a named seam; business gap-fill is `slice-implementer` work and needs the user's separate authorization (case B): read the declared hook signatures and
   the generated types the adapter touches (bounded), write the adapter behind a small interface
   with no business decisions, rebuild, retest. Never edit a managed file; never add behaviour the
   model does not state. Detail: `references/delivery-protocol.md` section 4.
7. **Report authored-UI parity.** Require explicit plan/runtime evidence for each authored screen,
   form, layout, template, toolbar, component, outlet, route, package/icon use, theme and persona view,
   or a blocking diagnostic or "not rendered" with the responsible tool version; silent fallback to
   default composition is a failure (Stage 4.24 rendered the default composition only).
8. **Drift:** `cratis render ... --check` (exit 6 pending); also compare the manifest's `semanticRevision`
   with a fresh render under the same inputs. For hand-written scope, `cratis screenplay conform` gives
   structural code-vs-model evidence (`references/drift-and-fallback-record.md`). For changes since verification,
   follow that reference's "Hand-written code changed since it was last verified" procedure.
9. **Hand off** the packet: verdict lines, spec mapping, edit requests for the modeler, fallback record
   entries, UI omissions, learning candidates (at most 3).

## Hand-written parts

Adapters and non-renderable scope follow the contract rules of `cratis-application-slice-conformance`
(field inventory, specification mapping, delta delivery, never weaken a spec-derived test, the
`done | partial | blocked` status). They are linked here, not duplicated. What this skill adds:

- Hand-written scope is `done` only when no specification lacks an executable equivalent
  (`cratis-application-slice-conformance` `references/build-prompts.md`).
- One scope per `slice-implementer` brief; the brief carries addresses and paths, not contents.
- Model-enforced rules realized in code (state-dependent rules) need a protected read or
  concurrency scope; never claim them from prose. Stage-rendered apps scaffold
  the Arc/Chronicle set in `cratis-stage-rendering-and-sandbox` `references/rendered-application.md`; API availability alone is not verified enforcement.
- Adapters that call outside systems follow `cratis-engineering-effect-boundaries`; slice
  specifications follow `cratis-application-slice-specifications`.
- Read conformance's `references/contract-and-precedence.md` for realization-note
  authority. Executable contract wins over prose; report contradictions.

## Rules

- Compiler contract: what Stage admits is decided by the installed cratis CLI, not by the standalone
  compiler (subsets: `cratis-screenplay-toolchain` `references/renderable-subset.md`).
- This skill changes no model. Model fixes are edit requests (address, change, rationale) to the owning session.
- Code reading is bounded: `Customizations/*`, the file and line a build or test error names,
  and the declared hooks. Never browse managed output, `bin/` or `obj/`.
- `description` and `documentation` reach rendered code only as XML documentation comments (Stage#178),
  never as behavior. A rule stated only in prose is unenforced: fallback record entry or target requirement,
  never "delivered".
- The `cratis run` sandbox's legacy EventModel engine (the default engine before CLI 3.73.0) enforces only simple literal validation rules and no
  authorization; it is no evidence for denial behaviour or for any other rule.
- Never remove `pii`, `secret`, authorization or rules to get a render through; report the gap.
- Generated .NET apps validate concept `matches` rules at runtime with the `Cratis.Screenplay.Matching` package (`PortableMatcher`),
  the matcher the compiler admitted; reference it at the same version as the Screenplay compiler.
  Requires Screenplay 4.241.0 (`screenplay --version`) for that package.
- Stop and ask only on a genuine contradiction between model, specs, descriptions and declared
  contracts, after reading the slice fully (`cratis-screenplay-modeling-lifecycle` `references/stop-or-assume.md`).
- Commit set after a delivery the user accepts: the destination's `.cratis-render.json`,
  managed output and `Customizations/`. The model and identity catalog were committed at P6.
  Never `.cratis-render/` (the journal directory) and never `.ai-work/`.
- Limits stated in this text are prompt policy unless a guard enforces them; say so.

## Hand-written query logic and re-render

Symptom: a today-relative query (overdue, due today) was written by editing the body of a generated query
file; the next `render` refused with `Managed artifact '<file>' was modified by the user; pass --force to
replace it.` Query filters are equality only and there is no `today` in a filter or projection, so this
logic is gap-fill. Cause: a managed file's hash is in `.cratis-render.json`, and an edited one blocks the
whole re-render. What to do:

- Never edit a managed file. Put the logic in a new unmanaged file, for example `Customizations/<Name>.cs`
  (compiled with the project, never replaced by `render`), with its own endpoint or query class, and record it
  as gap-fill (`references/gap-fill-handoff.md`).
- `--force` replaces the edited managed file with the generated one and discards the edit; use it only with
  approval that names the file.
- Screens reported as `PLAY0269` are information that the backend profile defers them. They are not
  proof of a rendered screen: check the running app (`cratis-screenplay-toolchain` `references/fast-path.md`)
  and list any authored screen that is missing as not rendered.

## Gate

Before reporting done:
- Source identity, tool versions, `--name` and destination are recorded.
- V5 is four lines: admission, publication, build (Debug), tests (Debug), each a result or `not run: <reason>`.
- Every diagnostic is classified; every non-renderable scope has a fallback record entry or the user's decision to stay in design mode.
- No managed file was edited; no `--force` without approval naming it.

## Verify

- Expected specifications versus generated classes versus passed, listed; a count is not a mapping.
- Fallback delivery: the fallback record entry maps specifications to tests, records the field inventory
  and divergences, and `cratis-application-slice-conformance` returned a status.
- A passing test does not claim runtime guarantees (namespace, subject, constraint scope, guarded
  reads, migrations, replay) or omitted UI were delivered.
- Re-render: drift line present. `Customizations/` changes name the seam and the model element served.
- Screen delivery: every authored screen/template/form/toolbar/component/outlet/route/package-icon use is either present in generated/runtime evidence or listed as not rendered/unsupported with the responsible tool version.

## Route near misses

Read [references/neighbouring-skills.md](references/neighbouring-skills.md) when the work is model changes, specification forms, tooling, renderer facts, refused scope, conformance or code review rather than render delivery.

## References (load on demand)

- `references/renderability-gate.md` - classify the probe's codes, estimate the blast radius.
- `references/delivery-protocol.md` - inputs, commands, reading results, `Customizations/`.
- `references/gap-fill-handoff.md` - the brief for `slice-implementer`, fallback record flow.
- `references/post-deploy-check.md` - after deployment, compare the model with a running namespace using `cratis screenplay conform-namespace`; read-only, with its exit codes.
- `references/drift-and-fallback-record.md` - drift, publication refusals, the fallback record, code changed since verification.
- Read `references/hand-written-changes.md` when investigating hand-written changes against the contract; it defines finding kinds, resolution routing and the report.
- Read `references/worked-example.md` when preparing the classification and report for a refused render; it contains a complete model.
- Read `references/provenance.md` when checking sources and attribution for the render workflow or gap-fill brief.

## Lineage

Draws on the earlier model render workflow and, for the gap-fill brief, on Martin Dilger and Nebulit GmbH's slice
build instructions (adapted closely); see `references/provenance.md`.
