---
name: cratis-screenplay-render-and-gap-fill
description: "Deliver an accepted Screenplay `.play` model as code: probe whether `cratis render` admits the whole model, publish, then report admission, publication, Debug build and Debug tests as separate results mapped to the generated specification classes; triage model fix versus capability gap; bounded `Customizations/` adapters; drift by manifest revision; and hand non-renderable scope to `slice-implementer` with the model as the contract. Use when an accepted model must become running code, when asking whether a model renders, or after a model change. Not for: changing the model (use `cratis-screenplay-slice-design`), Stage internals (use `cratis-stage-rendering-and-sandbox`), or checking hand-written code against its slice (use `cratis-application-slice-conformance`)."
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

Version pins live once in `cratis-screenplay-toolchain` `references/versions.md`; read it before
selecting the compiler and renderer for delivery: the CLI bundles its own Screenplay and Stage;
its compiler version differs from the standalone pin. Render options (`--scope`,
`--check`, `--package-set`) and exit codes were read at cli `v3.53.1` `Documentation/reference/screenplay.md`
and `cratis render --help`; admission at Stage `v4.89.1` (`SemanticVersionFeatures.cs:105`,
`SemanticVersionFeatureAdmission.cs:25-46`, `SemanticCratisAdmission.Automation.cs:15-123`). Stage's scaffold pins Arc and
Chronicle releases, which are not the latest (`cratis-stage-rendering-and-sandbox`
`references/rendered-application.md`). Source reading is not a new render run.

Runs: the worked example (`references/worked-example.md`) was probed with cratis 3.53.1 (observed; the
3.28.2/3.28.3 runs are history). With public CLI 3.43.0 (Stage 4.52.0) the canonical `ScreenComposition`
corpus was rendered and published successfully, and an MCP-edited copy was rendered, run and checked in
the browser; the CLI 3.40.7 browser acceptance run passed 51 assertions (Cratis/Screenplay#605). The AI
repository's `screenplay-mcp-transcript.ts` harness proves the real stdio proposal/apply path against the
packaged CLI. Guarded Close actions and double-click interactions were refused (`STAGE-SCENE-ACTION-001`,
`STAGE-SCENE-INTERACTION-001`) on that vector; since Stage 4.65 they render and run, and the codes
remain for a guard Stage cannot evaluate, which is never emulated (Cratis/Screenplay#671). UI-profile
rejection (`CLI-RENDER-007` to `-010`) and render-then-recover shipped in 3.41.0 (Cratis/cli#301). The
renderer facts are owned by `cratis-stage-rendering-and-sandbox`; this skill links them and never
restates the admission table.

## When to use / when not

- Use: an accepted model (P6, bound to a source identity) must become running code; "would this
  render?"; a re-render after a model change; a drift check; a build failure in `Customizations/`.
- Not for: changing the model (return edit requests to the modeler session); choosing the mode
  (the user); Stage internals and the sandbox (`cratis-stage-rendering-and-sandbox`); judging
  hand-written code against its slice (`cratis-application-slice-conformance`).

## Interview phase
**Skip if** the request names the model root, the application name, the destination and the
decision on non-renderable scope. Otherwise ask only what is missing:
1. **Which model, which accepted version?** (root, source identity from the lifecycle report).
2. **Application name** for `--name` (it is part of the application identity: keep it identical on every re-render).
3. **Destination.** Default `.ai-work/screenplay/<model-slug>/render-probe/<App>`; any other destination needs the user's intent.
4. **If admission fails:** stay in design mode, change the model for business reasons, or gap-fill by hand? (the three cases below).
5. **Seams:** outside systems, styles or packages the rendered app needs (`Customizations/`).

Unattended: assume visibly (probe destination, default name from the model root folder), never
assume an answer to 4, and record it as `blocked` for the user.

## The three cases (decide first, say which one applies)

| Case | Situation | What happens |
| --- | --- | --- |
| **A. Admission failure** | the probe reports blocking diagnostics | nothing was rendered or published by this run. Report the codes; the user chooses the next step (see "Procedure" 3) |
| **B. Generated base plus authorized gap-fill** | an admitted render (whole, or a `--scope` of the admitted slices) exists and the user separately authorized hand-written scope, or a seam the model names needs an adapter | the generated output stays managed and untouched; hand-written code lives in `Customizations/` or a separate project (seam adapters by this skill, business gap-fill by `slice-implementer`), serves named model elements only, never claims render admission, and V5 is reported for the generated base **with its scope stated** |
| **C. Fully hand-written delivery** | the scope cannot render and no generated base is wanted | `slice-implementer` delivers it with the model as the contract (`references/gap-fill-handoff.md`); V5 is `not run: no render` |

Evolved events are gap-fill too: Stage still refuses (4.89.1) any evolved event and its dependent scope with `STAGE-ESM-026`, because it cannot render Chronicle event-type migrations yet (Stage#204). Events at their initial revision still render. Hand-write the evolved events and their migrations per `cratis-chronicle-event-type-migration`, with the model as the contract.

Supported UUID/String generation strategies, v15 freshness and scalar/named `returns`
responses, fixtures and `then returns` render with CLI 3.57.0 / Stage 4.89.1. Unsupported generated
types (`STAGE-ESM-028`) and result combinations (`-038`) still require case A or C:
preserve the model and its specifications. See Stage's `references/admission.md`.
Supported event-triggered Automation slices also render as managed Chronicle reactors;
refused reactions, captures and Translate slices remain gap-fill.

Models with event sources, streams or command routes bind and reference-execute as ESM v8 on the standalone
tool, so report V3 and V4 from that tool. The pinned CLI/Stage pair
(`cratis-screenplay-toolchain/references/versions.md`) reaches v8 route checks
and construct admission through v15. Public/foreign events and production overrides
still refuse; semantic reaction/reducer filters and role/authenticated `runs as` render.
Claim-reading/opaque system gates, legacy actor/filter paths and mixed effects still refuse. Keep refused scope as case A/C,
never infer target support from binding. Read Stage's `references/admission.md` and
toolchain `references/sources-and-streams.md`.

**Mixed reaction effects.** A reaction that both `produces` events and `invokes` commands is refused by
Stage (`STAGE-ESM-037`, Stage#207) although Arc 22.72.0 and later run one reactor returning events
and commands (events appended first as one batch, then commands in order, no cross-effect
transaction; a retry can repeat the events). The scaffold now includes the mixed-result handler,
but Stage still refuses it because redelivery can duplicate events. `[OnceOnly]` excludes replay,
not retries. Keep the modeled reaction whole; gap-fill needs a proven retry-safe realization,
not splitting or dropping effects to get a render. Source: Stage
`v4.89.1:Documentation/reference/reaction-rendering.md:53-98`.

Two rules hold in every case: a customization never makes a rejected model renderable, and
a whole-application V5 is never claimed from a subset. `--scope` renders an admitted part (the whole
model still compiles and binds) and its V5 states the scope.

## Procedure

1. **Preconditions.** P6 has committed the accepted `.play` files and `.screenplay/identities.json`
   when present; uncommitted model changes return to P6 before delivery. Record the source identity (`cratis-screenplay-modeling-lifecycle`
   `references/verdicts-and-modes.md` "Source identity"), the model root, `--name`,
   `--project-name`, `--root-namespace` and target `cratis`. Take V1 and V3 from
   `cratis-screenplay-toolchain`; report each with tool and version. V3 from a different
   compiler version does not prove the cratis-bundled binder admits the model (the bundle is
   4.147.1, the standalone tool 4.149.0). `cratis screenplay validate --executable <root>` binds with
   the bundled compiler and writes nothing: use it as the bundled-binder check (toolchain `references/verdicts.md`).
2. **Probe.** `cratis render <model-root> --name <App> --destination <probe-dir> -o json`
   (exit 0 published, 5 refused with nothing published, 1 missing input); add `--check` to plan
   against the destination without writing (exit 6 changes pending) and `--scope <Module.Feature.Slice>`
   (repeatable) to render an admitted part. Classify every
   diagnostic with `references/renderability-gate.md`: model fix in the chosen mode,
   capability gap, tool skew or environment. Group by code and count; do not paste the list.
3. **When admission fails** (case A) present the options, the user decides:
   (a) keep design mode and record the gaps; (b) narrow to renderable mode, where the modeler
   changes the model for business reasons and **without** removing protection (`pii`,
   authorization, rules); (c) deliver the non-renderable scope by hand (case C), with a ledger
   entry (`references/drift-and-ledger.md`); (d) render the admitted part with `--scope` and gap-fill the rest
   (case B).
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
   structural code-vs-model evidence (`references/drift-and-ledger.md`).
9. **Hand off** the packet: verdict lines, spec mapping, edit requests for the modeler, ledger
   entries, UI omissions, learning candidates (at most 3).

## Hand-written parts

Adapters and non-renderable scope follow the contract rules of `cratis-application-slice-conformance`
(field inventory, specification mapping, delta delivery, never weaken a spec-derived test, the
`done | partial | blocked` status). They are linked here, not duplicated. What this skill adds:

- Hand-written scope is `done` only when no specification lacks an executable equivalent
  (`cratis-application-slice-conformance` `references/build-prompts.md`).
- One ledger scope per `slice-implementer` brief; the brief carries addresses and paths, not contents.
- Model-enforced rules realized in code (state-dependent rules) need a protected read or
  concurrency scope; never claim them from prose. Stage 4.89.1 rendered apps scaffold
  the Arc/Chronicle set in `cratis-stage-rendering-and-sandbox` `references/rendered-application.md`; API availability alone is not verified enforcement.
- Adapters that call outside systems follow `cratis-engineering-effect-boundaries`; slice
  specifications follow `cratis-application-slice-specifications`.
- Realization notes are the explicit realization requirements stated in a slice `description` or `documentation`
  (definition: `cratis-application-slice-conformance`, `references/contract-and-precedence.md`).
  Executable parts (specifications, mappings, constraints) win over prose, and a contradicting description is a model defect to report.

## Rules

- Compiler contract: what Stage admits is decided by the installed cratis CLI, not by the standalone
  compiler (subsets: `cratis-screenplay-toolchain` `references/renderable-subset.md`).
- This skill changes no model. Model fixes are edit requests (address, change, rationale) to the owning session.
- Code reading is bounded: `Customizations/*`, the file and line a build or test error names,
  and the declared hooks. Never browse managed output, `bin/` or `obj/`.
- `description` and `documentation` reach rendered code only as XML documentation comments (Stage#178),
  never as behavior. A rule stated only in prose is unenforced: ledger entry or target requirement,
  never "delivered".
- The `cratis run` sandbox enforces neither validation nor authorization on its default engine; it
  is no evidence for denial behaviour.
- Never remove `pii`, `secret`, authorization or rules to get a render through; report the gap.
- Stop and ask only on a genuine contradiction between model, specs, descriptions and declared
  contracts, after reading the slice fully (`cratis-screenplay-modeling-lifecycle` `references/stop-or-assume.md`).
- Commit set after a delivery the user accepts: the destination's `.cratis-render.json`,
  managed output and `Customizations/`. The model and identity catalog were committed at P6.
  Never `.cratis-render/` (the journal directory) and never `.ai-work/`.
- Limits stated in this text are prompt policy unless a guard enforces them; say so.

## Gate

Before reporting done:
- Source identity, tool versions, `--name` and destination are recorded.
- V5 is four lines: admission, publication, build (Debug), tests (Debug), each a result or `not run: <reason>`.
- Every diagnostic is classified; every non-renderable scope has a ledger entry or the user's decision to stay in design mode.
- No managed file was edited; no `--force` without approval naming it.

## Verify

- Expected specifications versus generated classes versus passed, listed; a count is not a mapping.
- Fallback delivery: the ledger entry maps specifications to tests, records the field inventory
  and divergences, and `cratis-application-slice-conformance` returned a status.
- A passing test does not claim runtime guarantees (namespace, subject, constraint scope, guarded
  reads, migrations, replay) or omitted UI were delivered.
- Re-render: drift line present. `Customizations/` changes name the seam and the model element served.
- Screen delivery: every authored screen/template/form/toolbar/component/outlet/route/package-icon use is either present in generated/runtime evidence or listed as not rendered/unsupported with the responsible tool version.

## Route near misses

- Changing the model, mode or slices: `cratis-screenplay-slice-design`, `cratis-screenplay-modeling-lifecycle`.
- Which specification forms Stage accepts: `cratis-screenplay-scenario-coverage`, `cratis-screenplay-specifications`.
- Verdict commands, subsets, versions: `cratis-screenplay-toolchain`.
- Renderer facts, publication recovery, sandbox: `cratis-stage-rendering-and-sandbox`.
- Refused automation or translation scope (the refused behavior is gap-fill): `cratis-screenplay-automations-and-translations`.
- Hand-written code against its contract: `cratis-application-slice-conformance`.
- Code review of delivered code: `cratis-code-review`.

## References (load on demand)

- `references/renderability-gate.md` - classify the probe's codes, estimate the blast radius.
- `references/delivery-protocol.md` - inputs, commands, reading results, `Customizations/`.
- `references/gap-fill-handoff.md` - the brief for `slice-implementer`, ledger flow.
- `references/drift-and-ledger.md` - drift, publication refusals, the fallback ledger.
- Read `references/worked-example.md` when preparing the classification and report for a refused render; it contains a complete model.
- Read `references/provenance.md` when checking sources and attribution for the render workflow or gap-fill brief.

## Lineage

Draws on the earlier model render workflow and, for the gap-fill brief, on Martin Dilger and Nebulit GmbH's slice
build instructions (adapted closely); see `references/provenance.md`.
