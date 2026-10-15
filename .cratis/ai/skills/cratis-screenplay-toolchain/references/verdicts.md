<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/verdicts.md -->
# Verdict commands, V1 to V5

## Contents

- V1: authorable
- V2: executable diagnostics
- V3: binding-ready
- V4: reference specifications run
- V5: rendered and target-verified
- Report format
- Example gate (for skill and documentation authors)

The five verdicts are independent results, not a ladder: each proves only its own layer,
and each is either a result or "not run" with the reason. The verdict names are frozen
(`cratis-screenplay-modeling-lifecycle` owns when each is required; this file owns how to
obtain it). Tool and version facts: `versions.md`. No script ships with this skill: V2 and
V3 are read from the MCP server or from a render, V1 and V5 from the CLIs.

| Verdict | Meaning |
| --- | --- |
| V1 | authorable: compiles, warnings as errors |
| V2 | executable diagnostics: the binder's diagnostics were read |
| V3 | binding-ready: the executable model binds with no blocking executable diagnostics |
| V4 | reference specifications run |
| V5 | rendered and target-verified: admission, publication, build and tests, each separately |

## V1: authorable

| Tool | Command | Pass | Fail |
| --- | --- | --- | --- |
| Standalone (preferred; pin: `versions.md`) | `screenplay <model-root> --warnaserror --no-color` | exit 0 and a nonzero compiled file count | exit 1 defects; exit 2 could not run/invalid arguments or input |
| cratis (fallback) | `cratis screenplay validate <model-folder> --warnings-as-errors -o json-compact` | exit 0 | exit 5; exit 1 means missing path or no files |

- Validate the complete application root, not an independently selected fragment.
  Standalone file mode follows imports; folder mode includes all files. The CLI
  file-import limitation below was a 3.28.2 probe, not a current tool claim.
- Require a nonzero file count; empty input is not a model-validation pass.
- Optional `--scope <Module.Feature.Slice>` compiles the whole application but
  reports descendants and direct dependent declarations, not transitive impact.
  Exit/`--warnaserror` apply to that reported set; inspect the separate whole
  application counts before a whole-model claim. Unknown/ambiguous scopes exit 2.
- `--check <names>` (repeatable) or `--check all` opts into completeness warnings
  including PLAY0530–PLAY0537, purposes (PLAY0602–0606) and privilege (PLAY0652).
  Whole-source errors skip them; a skipped check is not passed.
  Names/codes, coverage limits and editor support: `diagnostics.md`.
- Warnings are failures here. PLAY0029 (a construct dropped silently) and the unresolved-name
  warnings are the defects this verdict exists to catch.
- Syntax and consistency checks (PLAY0282 to PLAY0294) run; binding does not.
- Report: `V1 pass (screenplay 4.139.0, 3 files)`, `V1 fail: PLAY0029 a.play:12 ...`.

## V2: executable diagnostics

Needs an MCP connection (V2 and V3 belong to the session that owns MCP; a subagent without
MCP reports `V2 not run: no MCP in this agent` unless the brief supplies fresh output). Without
MCP, a binding check exists: `cratis screenplay validate --executable <root>` binds with the
**bundled** compiler (CLI 3.57.0 bundles Screenplay 4.147.1, not standalone 4.149.0), never
renders or writes files, and exits 5 with "The model does not bind into an executable semantic
model" when it does not. Report it as `V3 (bundled 4.137.0 via cratis validate --executable)`; it
gives binding errors, not the V2 `executable-diagnostics` view, so V2 stays MCP-only.

1. Start the server with a fixed root: `screenplay mcp <model-folder>` or `cratis screenplay
   mcp` in a project with `.cratis/ai.json` (see `versions.md`, roots bug). Send
   `initialize`, then `notifications/initialized`.
2. `open-workspace`. The result carries `readiness` (`authoringAccepted`, `executableReady`)
   and `executableDiagnosticsCount`.
3. Read the diagnostics: `read-workspace` with `view: "executable-diagnostics"` and the
   current `expectedRevision` (page with `offset`; paging beyond the first page needs the
   source revision).
4. Report the codes with counts, blocking (error severity) apart from informational. A list
   with blocking entries is a valid V2 result (`V2 read: 6 blocking, PLAY0268 x3 filter query,
   enum compliance ...`); it is the evidence for a "V3 blocked" report. The view also lists
   information-severity diagnostics (PLAY0269 deferred UI, PLAY0270 authoring metadata such as
   descriptions, personas and screens) on models that bind; they do not block.

`authoringAccepted: true` only says the source compiled (or the root is empty). It says
nothing about print or parse fidelity or identity continuity.

## V3: binding-ready

V3 holds when `executableReady` is true and V2 returned **no blocking** (error-severity)
executable diagnostics; informational PLAY0269/PLAY0270 entries are reported separately and
do not block. The ESM then binds (and `read-workspace view=executable-model` reports `available: true` with a
`modelRevision`; avoid reading the whole model bytes just for the revision unless a
comparison needs it). Binding is what makes a model renderable at all. V3 means "binds",
never "specifications pass".

- Report the ESM version the model needs: `V3 ready (screenplay 4.139.0, ESM v6)`,
  `V3 blocked: PLAY0268 (enum compliance) - design scope kept`.
  Bare `pii`/`personal` and `secret` bind as v13 on standalone 4.142.0; reasons
  remain report-only PLAY0270. Enum/identity compliance and personal or explicitly
  subject-scoped secret event values with an explicit `subject` mark still refuse
  PLAY0268 at binding only. CLI 3.57.0 bundles Screenplay 4.147.1 / Stage 4.89.1 and renders v13
  concept protection (not protected identities/enums or explicit subject lineage).
  V3 does not prove rendering.
- A blocked V3 is not repaired by deleting protection or domain rules. Never remove `pii`,
  `secret`, authorization, list queries, automations or rules to reach V3; record the
  slice and the code in the gap list.
- A model is executable only on the compiler that bound it. V3 does not carry
  to render admission: the pinned CLI (`versions.md`) passes through ESM v10 to Stage construct
  checks; newer models receive CLI-RENDER-004. Public publication and reaction
  identity/filters/production overrides still receive typed Stage refusals.
  The old 3.28.x/Stage 4.24.x probes remain historical in the subset references.

## V4: reference specifications run

Run `screenplay test <root> [--filter <exact-address>] [--format text|json]` on
the standalone pin, or MCP `run-specifications`. The compile command alone still does not
execute scenarios. Reference execution is in-memory: no external services or
opaque code run. Record route, expected versus discovered/selected counts,
passed, failed and unsupported outcomes. Exit 0 means all selected passed; 1
failed, 2 could not run/invalid input or selection, 3 unbound or unsupported.
Unknown filters refuse rather than select zero. No discovered specifications
is not proof of coverage. `test` rejects `--no-color` (exit 2).

MCP selectors are `specification` (exact address/id) and `scope` (prefix);
both supplied mean intersection. `offset`/`limit` page results; continuation
requires `expectedSourceRevision`. On **Screenplay 4.145.0 and later**, pass
`proposalId` between proposal review and `apply` to run the retained candidate
read-only. Without that argument it runs the workspace. A candidate
V4 belongs only to its returned source revision, not to the disk model.
Unknown/applied/discarded/reopened proposals give `UnknownProposal`.
A statically impossible `then` reports PLAY0285: the whole model is unbound,
exit 3, and no specifications run. Details and source evidence:
[reference execution](../../cratis-screenplay-model-authoring/references/mcp-tools.md#run-reference-specifications).

The reference runner returns unsupported for a specification that reads a reducer-built read
model and for any construct whose evaluation needs an opaque body.

**Rendered Debug tests are never V4.** A rendered application is a different engine: Stage
4.24 admits pure reducer bodies and generates reducer replay and assertions in its Debug
tests, so the two engines can differ (a pure reducer body is a concrete case). Report those
outcomes only under V5.tests, labelled target-test evidence. The `cratis/stage-specrunner` job
(Docker, opt-in; behaviour on ESM v4 to v6 unverified) runs Stage's own semantic executor
(`SemanticSpecificationExecutor`, Stage `v4.24.0` `Source/SpecRunner/Program.cs`), not the
reference route: report its outcomes on a separate line, "Stage semantic engine (target-engine
evidence)", never as V4 and not as V5.tests.
Without a reference route: `V4 not run: no route`.

## V5: rendered and target-verified

`cratis render` is the entry point; it neither builds nor tests. Report four separate
results, plus runtime when it was exercised:

1. **Admission**: the render plan was accepted, or the STAGE-ESM / PLAY / CLI-RENDER codes
   that rejected it (nothing is rendered on rejection).
2. **Publication**: what was written, unchanged or refused (`.cratis-render.json`,
   `.cratis-render/journal.json`; `recovered: true` means stop and reconcile).
3. **Build**: Debug build of the destination (`dotnet build`).
4. **Tests**: Debug tests of the destination (`dotnet test`). Generated specifications are
   compiled in Debug only; map results by generated spec class (`when_<snake>` in namespace
   `<Slice ns>.when_<snake>`, `when_<snake>_is_queried`, `when_<snake>_is_projected`) back to
   the `.play` specification, not by test counts.

Never claim a whole-application V5 from a rendered subset. Render workflow, ownership
and gap-fill: `cratis-screenplay-render-and-gap-fill` and `cratis-stage-rendering-and-sandbox`.
What Stage admits: `renderable-subset.md`.

## Report format

One line per verdict, tool and version first, evidence second:

```text
V1 pass (screenplay 4.139.0, 3 files)
V2 read (screenplay 4.139.0 via MCP): 0 blocking, 3 informational (PLAY0270)
V3 ready (screenplay 4.139.0, ESM v3)
V4 not run: no route
V5 admission pass (cratis 3.53.1, Stage 4.73.1); publication written 14 files; build not run; tests not run (a sample line, not a result)
```

Rules: a clean verdict never implies a later one; a verdict names the **source identity** it
ran on (commit plus the digest from the source-identity helper (`cratis-screenplay-modeling-lifecycle` `references/verdicts-and-modes.md` "Source identity")); a tool or
source change makes earlier verdicts stale.

## Example gate (for skill and documentation authors)

Historical corpus fences were checked with standalone 4.68.0 and CLI 3.28.2.
Do not promote those results to current pins without rerunning. Check each changed
complete fence with the pinned standalone tool (`versions.md`) `--warnaserror`; run `screenplay test` for
specification examples, and verify expected-diagnostic examples against their
claimed codes. Where bundle compatibility matters, also check the pinned CLI (`versions.md`).
The `// Needs the standalone screenplay compiler (ESM v6)` first-line marker belonged to cratis
before 3.28.2, which could not validate those fences; no fence carries it now, and a
checker that still skips marked fences under `cratis` skips nothing. Examples meant to execute are also opened through MCP and
must be `executableReady`. No example uses `numbers exact`.

Edit strategy for `.play` changes (typed identity-preserving operations preferred, bounded text
edits for address-preserving changes) is in
`SKILL.md`; the MCP edit procedure is in `cratis-screenplay-model-authoring`.
