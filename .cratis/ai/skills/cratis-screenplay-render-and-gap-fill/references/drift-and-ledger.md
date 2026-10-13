<!-- cratis-ai-managed: skills/cratis-screenplay-render-and-gap-fill/references/drift-and-ledger.md -->
# Drift, publication refusals and the fallback ledger

## 1. Drift: the model changed, the output was not re-rendered
The ownership manifest `.cratis-render.json` at the destination records `semanticRevision`
(`rev1:<sha256>`), the target, the renderer, the application name and a hash per managed file.

Facts verified with `cratis render` 3.28.2 on one model (historical probe; still how identity works): the same files rendered under the names
`Marina` and `Harbour` gave different `semanticRevision` values, so **the application name is part
of the revision**. The MCP workspace names the application after the model root folder (without
`.screenplay/identities.json`), so its `modelRevision` is not comparable with a render's manifest
revision by assumption. Without `.screenplay/identities.json` it changes when the root folder is
renamed (relocating the root while keeping its name does not); source positions and descriptions do
not change it, and a file move changes it only if it changes logical placement or application identity.

**First probe: `cratis render <root> --name <App> --destination <dir> --check`** (CLI 3.44.0 or later; add
the `--scope` list used for delivery). It plans against the real destination, writes nothing and runs
no recovery: exit 0 current, 6 changes pending, 5 planning failure, refusal or pending recovery.
Documentation edits also change managed-file hashes, so they show as pending changes. `--check`
replaces the throwaway `-drift` render below; the manifest comparison stays as a cross-check.

Compare in this order:
1. **Source identity** (`cratis-screenplay-modeling-lifecycle` `references/verdicts-and-modes.md`):
   compute it for the current model and compare it with the identity recorded when the output was
   rendered. Equal: the same model bytes, whatever git says. Different: the output is behind the model.
2. **Manifest revision** (when `--check` is not available): render the current model with the **same** `--name` and inputs into a
   fresh probe folder (`.../render-probe/<App>-drift`) and compare its manifest `semanticRevision`
   with the published one. Equal: no semantic change since publication. Different: re-render and review the receipt.
3. Equal revisions prove neither customization conformance nor faithful UI.
4. **Hand-written scope against the accepted model:** `cratis screenplay conform <model-root> --project <path>`
   (CLI 3.42.0 or later; Arc, Marten and Critter Stack code) compares the declarations recovered from
   code with the model by exact structural address. Exit 0 no blocking findings, 1 defects (code-only
   declarations, shape mismatches), 2 could not run or incomplete coverage; model-only members are
   informational. It is **structural drift evidence only**, never a behavioral equivalence verdict and
   never V4 or V5; it needs the same generation options (`--domain`, `--feature-root`, `--module`,
   `--skip-segments`, `--modules-from-namespace-roots`) that reproduce the authored placement, or
   everything looks missing. MSBuild evaluates the project (use a trusted clone) and the bundle's
   `Cratis.Arc.Screenplay` is the version in `cratis-screenplay-toolchain` `references/versions.md`, so recovery behaves like that adapter. It does not replace
   `cratis-application-slice-conformance`, which reconciles specifications, rules and behavior.
   Whether it is meaningful on a Stage-rendered application is unverified.

Use the same toolchain as the render; revisions from another Screenplay version are not
comparable by assumption. A dirty git flag or commit is context, never a revision.

## 2. Publication refusals (code changed under the model)

| Refusal | Meaning | Do |
| --- | --- | --- |
| managed file "modified by the user" | someone edited generated output | the model is authoritative. Diff the file against a fresh probe render; if the edit expresses wanted behaviour, move it into the model (edit request) or a `Customizations/` seam; then ask for approval to re-render with `--force` for that destination |
| unmanaged existing file on a planned path | a hand-written file collides with a planned artifact | move or rename the hand file (ask); never delete it unasked |
| modified stale managed file | a file the model no longer plans was edited | as a modified managed file; it will not be removed |
| manifest identity change (target, renderer, application name) | different render inputs | restore the original inputs, or treat it as an explicit migration the user approves |

`--force` replaces modified **active** managed files only; it never overwrites unmanaged files or
removes modified stale ones. It needs approval naming the effect and destination (a direct user
request already naming them counts); ask again only if the target or consequence expands.
Re-render checklist: `cratis-stage-rendering-and-sandbox` `references/rendered-application.md`.

## 3. Build breaks in `Customizations/` after a re-render
A generated contract changed. Read the error line and the generated type it names, fix the
adapter, rebuild. Never edit the generated file.

## 4. Fallback ledger
One file per model: `.ai-work/screenplay/<model-slug>/fallback-ledger.md`, untracked. A durable
record belongs in repository documentation or an issue, not in the ledger file. One entry per
non-renderable scope:

| Field | Content |
| --- | --- |
| id | `F<n>` |
| scope | slice addresses (`Module/Feature/Slice`) and their specifications |
| blocking codes | from the probe, grouped (`PLAY0268` list query x2, `STAGE-ESM-016`) |
| class | capability gap / tool skew / model fix pending |
| decision | stay design / renderable-mode change (user, date) / implementer |
| fallback location | project and folder of the hand-written code |
| spec mapping | `.play` specification to test name, one line each; missing tests listed |
| field inventory | `n/n` command, event and read-model properties mapped; extras: none, or listed with reason |
| realization requirements applied | explicit realization notes from slice `description` or `documentation` applied, one line each; other description prose used as a hint is listed separately |
| spec delta | at re-delivery: specifications added, changed or removed since `last verified`, each with test status |
| divergences | where the code deliberately differs from the model, with reason and approver |
| revisit trigger | issue and condition (for example "Stage#58 ships list queries") |
| last verified | source identity and date the mapping was checked |

When a revisit trigger fires, probe again; if the scope now renders, plan the migration from
hand-written code to managed output with the user.
