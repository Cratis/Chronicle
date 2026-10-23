<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/verdicts.md -->
# Verdict commands, V1 to V5

## Contents

- V1: authorable
- Declared validation gate (`screenplay.json`)
- Release-baseline gate (independent of V1–V5)
- V2: executable diagnostics
- V3: binding-ready
- V4: reference specifications run
- V5: rendered and target-verified
- Exit codes at a glance
- Model notes gate
- Revisions and descriptions
- Report format
- Example gate (for skill and documentation authors)

The five verdicts are independent results, not a ladder: each proves only its own layer,
and each is either a result or "not run" with the reason. The verdict names are frozen
(`cratis-screenplay-modeling-lifecycle` owns when each is required; this file owns how to
obtain it). No script ships with this skill: V2 and V3 are read from the MCP server or from a
render, V1 and V5 from the CLIs. Read installed versions with `screenplay --version` and
`cratis --version`; an older executable earlier on `PATH` shadows a newer install, so check
`which -a cratis` too.

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
| Standalone (preferred) | `screenplay <model-root> --warnaserror --no-color` | exit 0 and a nonzero compiled file count | exit 1 defects; exit 2 could not run/invalid arguments or input |
| cratis (fallback) | `cratis screenplay validate <model-folder> --warnings-as-errors -o json-compact` | exit 0 | exit 5; exit 1 means missing path or no files |

- Validate the complete application root, not an independently selected fragment.
  Standalone file mode follows imports; folder mode includes all files.
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
- A `screenplay.json` at the model root supplies checks, warnings-as-errors, scope and open-questions
  mode to both commands above, so they need not repeat those flags; drop `--warnaserror` only when
  `--print-gate` shows `warningsAsErrors: true`. Read the effective gate before
  reporting (next section).
- Report: `V1 pass (screenplay <version>, 3 files)`, `V1 fail: PLAY0029 in a.play, line 12`.

## Declared validation gate (`screenplay.json`)

A repository declares its V1 gate once, in an authored `screenplay.json` at the model root,
beside the tool-owned `.screenplay/`. Prefer it to repeating `--check`, `--warnaserror`,
`--scope` and `--open-questions` in every script: every run then enforces the same gate.
`screenplay.json` is the only file there people write; no tool creates or edits it.

```json
{
  "$schema": "https://raw.githubusercontent.com/Cratis/Screenplay/main/Documentation/screenplay/screenplay-v1.schema.json",
  "validation": {
    "checks": ["field-origins", "query-keys", "navigation"],
    "warningsAsErrors": true,
    "scope": "Workspaces.Tracking",
    "openQuestions": "fail"
  }
}
```

- Every member is optional. A `checks` item is one check name, diagnostic code or `all`
  (never a comma list); `all` expands to every check the running tool supports, so an upgrade can
  strengthen the gate, while an explicit list pins it. `openQuestions` is `report` or `fail`;
  omitted means notes are not evaluated.
- The model root is the nearest folder at or above the target (a file's parent) that holds
  `screenplay.json` or `.screenplay/`. The search includes but never crosses the first folder with
  `.git`; with no match the target folder is the root and the defaults apply. Nested models keep
  their own gates; imports never affect the search.
- A command-line option replaces only its own setting. Omitting `--warnaserror` keeps
  `warningsAsErrors: true`, and any `--check` replaces the file's whole list. `--no-gate-file`
  ignores the file, even an invalid one; a relaxed run is not the repository's gate, so say so.

| Need | Standalone | `cratis screenplay validate` |
| --- | --- | --- |
| Effective gate and the source of each value, no compile (exit 0 unless the file is refused) | `screenplay <root> --print-gate` (text) | `--print-gate` (text, or the gate object under `-o json`) |
| Machine-readable verdict | `--format json`: one document with `diagnostics`, `openQuestions`, `refusal`, `exitCode`, `gate` | `-o json-compact`: the summary carries `gate` |
| Ignore the file | `--no-gate-file` | `--no-gate-file` |

Each gate value reports `commandLine`, `file` or `default` as its source. Report the gate with
the verdict: `V1 pass (screenplay <version>, gate screenplay.json: field-origins, query-keys,
warnings as errors, 3 files)`.

**A refused gate file is "could not run", never a pass or a fail.** Malformed JSON, a duplicate
member, an unknown key, an unknown check name or a wrong type is refused as invalid, and a
directory, symlink or unreadable file is refused as unreadable; neither is ignored or repaired. The
standalone tool exits **2** and the CLI exits **1**, each with `path:line:column`. Report
`V1 not run: gate file refused (<path:line:column>)`, fix the file and rerun.
A scope declared in the file combined with `cratis screenplay validate --executable` also exits 1:
check binding separately or pass `--no-gate-file` and name it.

Requires Screenplay 4.165.0 or later (`screenplay --version`) for `screenplay.json`, `--format`,
`--print-gate` and `--no-gate-file`; an older tool does not read the file and runs without the
gate, so a clean result there says nothing about it. `cratis screenplay validate` reads the file
from Cratis CLI 3.65.0 or later (`cratis --version`); an older CLI ignores it, so use the standalone
tool for the gate.

## Release-baseline gate (independent of V1–V5)

Run `screenplay baseline check <model-root>` before accepting edits to released event
contracts: exit 0 matching, 1 changed or missing revision, 2 cannot check or unreadable. An
absent baseline is not proof of release admission. In the release job,
`screenplay baseline admit <model-root> --release <label>` records revisions; commit the
sidecar. Neither command changes canonical ESM or substitutes for migration or target tests.
Without a baseline an explicit event mode is still required: unadmitted revisions default to
in-place edits, admitted revisions accept only an explicit `newGeneration`. Never hand-edit the
sidecar except for a reviewed correction.
Preconditions and MCP review: model-authoring
[release baseline](../../cratis-screenplay-model-authoring/references/release-baseline.md).

Requires Screenplay 4.153.0 or later (`screenplay --version`) for `baseline`; with an older tool, report the gate as not run.

## V2: executable diagnostics

Needs an MCP connection (V2 and V3 belong to the session that owns MCP; a subagent without
MCP reports `V2 not run: no MCP in this agent` unless the brief supplies fresh output). Without
MCP, a binding check exists: `cratis screenplay validate --executable <root>` binds with the
**bundled** compiler, never renders or writes files, and exits 5 with "The model does not bind
into an executable semantic model" when it does not. Report it as
`V3 (bundled compiler via cratis validate --executable)`; it gives binding errors, not the V2
`executable-diagnostics` view, so V2 stays MCP-only.

1. Start the server with a fixed root: `screenplay mcp <model-folder>` or `cratis screenplay
   mcp` in a project with `.cratis/ai.json`. Send `initialize`, then
   `notifications/initialized`.
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

Requires Cratis CLI 3.46.1 or later (`cratis --version`) to start `cratis screenplay mcp --project-root <project>` without creating a model folder; older CLIs create the resolved root at startup.

Requires Screenplay 4.63.2 or later (`screenplay --version`) to initialize an MCP server without a fixed root; with an older tool, always pass the root as an argument.

`authoringAccepted: true` only says the source compiled (or the root is empty). It says
nothing about print or parse fidelity or identity continuity.

## V3: binding-ready

V3 holds when `executableReady` is true and V2 returned **no blocking** (error-severity)
executable diagnostics; informational PLAY0269/PLAY0270 entries are reported separately and
do not block. The ESM then binds (and `read-workspace view=executable-model` reports `available: true` with a
`modelRevision`; avoid reading the whole model bytes just for the revision unless a
comparison needs it). Binding is what makes a model renderable at all. V3 means "binds",
never "specifications pass".

- Report the ESM version the model needs: `V3 ready (screenplay <version>, ESM v6)`,
  `V3 blocked: PLAY0268 (enum compliance) - design scope kept`.
  Bare `pii`/`personal` and `secret` bind as concept compliance; reasons remain
  report-only PLAY0270. Enum/identity compliance and personal or explicitly
  subject-scoped secret event values with an explicit `subject` mark still refuse
  PLAY0268 at binding only. V3 does not prove rendering.
- A blocked V3 is not repaired by deleting protection or domain rules. Never remove `pii`,
  `secret`, authorization, list queries, automations or rules to reach V3; record the
  slice and the code in the gap list.
- A model is executable only on the compiler that bound it. V3 does not carry to render
  admission: the bundled renderer admits only ESM versions it knows and answers newer models
  with CLI-RENDER-004. Public publication and reaction identity, filters and production
  overrides still receive typed Stage refusals (`renderable-subset.md`).

## V4: reference specifications run

Run `screenplay test <root> [--filter <exact-address>] [--format text|json]` on
the standalone tool, or MCP `run-specifications`. The compile command alone does not
execute scenarios. Reference execution is in-memory: no external services or
opaque code run. Record route, expected versus discovered/selected counts,
passed, failed and unsupported outcomes. Exit 0 means all selected passed; 1
failed, 2 could not run/invalid input or selection, 3 unbound or unsupported.
Unknown filters refuse rather than select zero. No discovered specifications
is not proof of coverage. `test` rejects `--no-color` (exit 2).

MCP selectors are `specification` (exact address/id) and `scope` (prefix);
both supplied mean intersection. `offset`/`limit` page results; continuation
requires `expectedSourceRevision`. Pass `proposalId` between proposal review and `apply`
to run the retained candidate read-only; without that argument the call runs the workspace.
A candidate V4 belongs only to its returned source revision, not to the disk model.
Unknown/applied/discarded/reopened proposals give `UnknownProposal`.
A statically impossible `then` reports PLAY0285: the whole model is unbound,
exit 3, and no specifications run. Details:
[reference execution](../../cratis-screenplay-model-authoring/references/mcp-tools.md#run-reference-specifications).

Requires Screenplay 4.145.0 or later (`screenplay --version`) for `proposalId` on `run-specifications`; with an older tool, apply the proposal before running specifications.

The reference runner returns unsupported for a specification that reads a reducer-built read
model and for any construct whose evaluation needs an opaque body.

**Rendered Debug tests are never V4.** A rendered application is a different engine: Stage
admits pure reducer bodies and generates reducer replay and assertions in its Debug
tests, so the two engines can differ (a pure reducer body is a concrete case). Report those
outcomes only under V5.tests, labelled target-test evidence. The `cratis/stage-specrunner` job
(Docker, opt-in) runs Stage's own semantic executor, not the reference route: report its
outcomes on a separate line, "Stage semantic engine (target-engine evidence)", never as V4
and not as V5.tests.
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

## Exit codes at a glance

| Layer | Route | Result |
| --- | --- | --- |
| Source validation | `screenplay <root> --warnaserror --no-color` | 0 clean; 1 reported defects; 2 could not run or invalid arguments or input, including a refused `screenplay.json`. `--scope` limits the reported set, not compilation |
| Declared gate | `screenplay <root> --print-gate`; `--format json`; `--no-gate-file` | Reads `screenplay.json`; `--print-gate` exits 0 without compiling unless the file is refused; a gate-promoted warning or an open question in `fail` mode exits 1 |
| Completeness | add `--check <name>[,<name>]` (repeatable) or `--check all` | Opt-in warnings; whole-source errors skip checks, not pass them |
| CLI source validation | `cratis screenplay validate <root> --warnings-as-errors -o json-compact` | Read the producer exit and the JSON errors, warnings and file count; 5 validation error (including gate-promoted warnings), 1 invalid input (including a refused `screenplay.json`) |
| Binding | MCP workspace readiness and `executable-diagnostics` | V2 reads diagnostics; V3 requires binding with no blocking diagnostics |
| Reference specifications | `screenplay test <root> [--filter <address>] [--format text\|json]`, or MCP `run-specifications` (`specification`, `scope`, `offset`, `limit`, `expectedSourceRevision`, `proposalId`) | 0 all selected passed; 1 failed; 2 invalid arguments, input, selection or IO; 3 unsupported or unbound. Counts of zero do not prove coverage |
| Release baseline | `screenplay baseline admit <root> --release <label>`; `screenplay baseline check <root>` | 0 success; 1 changed or missing revision; 2 unavailable, unreadable or invalid input or write failure |
| Rendering | `cratis render` | Separate admission, publication, target build and target tests (V5) |

Source validation does not bind or run attached code. Reference execution is in-memory and
does not run opaque attachments or external services. CLI-derived specification IDs need not
match persisted MCP identities after renames; use MCP for identity-stable selection. A folder
compiles as one application: a standalone file includes its imports, not unimported siblings,
so keep the application root as the input for scoped checks and never treat a clean fragment
or scoped result as whole-application success.

MCP server launch: `screenplay mcp <model-root>` or `cratis screenplay mcp <model-root>`.
Discover `tools/list`, views and schemas rather than counting tools. Connection, worktree
switching and root-state conflicts: `cratis-screenplay-model-authoring`. The standalone tool
and the compiler bundled in the CLI can differ in release, so name the tool with every
verdict and never transfer a verdict between them without rerunning the same inputs.

## Model notes gate

```shell
screenplay <model-folder> --open-questions report --no-color
screenplay <model-folder> --scope Library.Loans.CheckIn --open-questions fail --no-color
```

`report` emits info `model-note.open-question` findings without failing for questions.
`fail` exits **1** when the selected set has an open question; unreadable notes exit **2**
when the flag is given. Compilation still runs with its own verdict; question findings are
MCP/CLI only, **no PLAY code**. With no flag the sidecar is ignored, and `--warnaserror` does
not gate questions. Tasks and ordinary notes do not fail this gate.

Scope includes descendants and ancestor notes (application notes too). An unscoped report
includes orphaned open questions. Absent identity state reports all open questions unscoped as
orphaned with `identity-state-absent`, rather than hiding them behind a scope. Notes never
change ESM or V1–V5 verdicts. Read model-authoring
[model notes](../../cratis-screenplay-model-authoring/references/model-notes.md) when
recording, resolving, paging or reviewing orphan impact.

Requires Screenplay 4.144.0 or later (`screenplay --version`) for `--open-questions`. `cratis screenplay validate` accepts the same option from Cratis CLI 3.60.0 or later (exit 5 when a question remains in `fail` mode, 1 when notes are unreadable); with an older CLI use the standalone tool. `validation.openQuestions` in `screenplay.json` selects the mode without the flag.

## Revisions and descriptions

`modelRevision` exists only when binding succeeds; it is canonical semantic
identity, not source identity. Source positions and report-only metadata can
change source/workspace revisions without changing it. Persisted catalogs
preserve assigned identities; do not reconstruct them from names.

A render manifest's `semanticRevision` is a different provenance value, made
with the render name and bundled compiler. Do not compare it directly to MCP
`modelRevision`. Compare successive renders with the same identity and inputs, or use
`cratis render ... --check`. Descriptions and event documentation render as XML
doc comments, so editing them changes managed-file hashes (a `--check` exit 6) but not the
executable model revision.

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

Check each changed complete fence with the standalone tool and `--warnaserror`; run
`screenplay test` for specification examples, and verify expected-diagnostic examples against
their claimed codes. Where bundle compatibility matters, also check the CLI. Examples meant to
execute are also opened through MCP and must be `executableReady`. No example uses
`numbers exact`.

Edit strategy for `.play` changes (typed identity-preserving operations preferred, bounded text
edits for address-preserving changes) is in `SKILL.md`; the MCP edit procedure is in
`cratis-screenplay-model-authoring`.
