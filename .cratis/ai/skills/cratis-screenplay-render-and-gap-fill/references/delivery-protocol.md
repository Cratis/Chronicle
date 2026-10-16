<!-- cratis-ai-managed: skills/cratis-screenplay-render-and-gap-fill/references/delivery-protocol.md -->
# Delivery protocol

## Contents

- 1. Inputs to collect before the first command
- 2. Commands
- 3. Reading results
- 4. Customizations (bounded)
- 5. Done check (before reporting)
- 6. Stop and ask
- Interview batch for render delivery

The render phase proves and publishes; it does not write business behaviour. Run one phase
per tool call and read each result from its exit status, never from a grep of the log.

## 1. Inputs to collect before the first command
- **Source identity** of the accepted model: the commit plus the digest of the explicit input
  manifest (`cratis-screenplay-modeling-lifecycle` `references/verdicts-and-modes.md`, "Source
  identity"). A dirty flag is never a revision. Bind the whole run to it; if it changes, start over.
- The **model root**. Use a dedicated folder: implementation attachments are read from it.
- **Render inputs:** `--name` (application identity, required for plain source, a C# identifier),
  `--project-name`, `--root-namespace`, `--target cratis`, and `--scope <Module[.Feature][.Slice]>` (repeatable) when
  only an admitted part is delivered; record the scope list with the source identity. Keep them identical across
  re-renders; changing the name or target makes publication refuse as an identity change.
- **Destination:** `.ai-work/screenplay/<model-slug>/render-probe/<App>` unless the user named another.
- Which modeled screens, personas and seams exist (UI omission list, section 4).

## 2. Commands

| Phase | Command | Result is |
| --- | --- | --- |
| Admission and publication | `cratis render <root> --name <App> --destination <dir> [--project-name <P>] [--root-namespace <N>] -o json` (forward every option recorded in section 1) | exit 0 published (receipt on stdout), 5 refused (diagnostics, nothing published), 1 missing input |
| Drift or pending-change check | the same command plus `--check` (and `--scope`, `--force` as used) | no write, no recovery: exit 0 current, 6 `changes_pending`, 5 planning failure, refusal or pending recovery |
| Bundled-binder check | `cratis screenplay validate --executable <root>` | exit 5 with "does not bind into an executable semantic model" when it does not; never renders or writes. Uses the bundled compiler, not the standalone pin |
| Debug build | `dotnet build <dir>/<Project>.csproj -c Debug` | the build's exit status |
| Debug tests | `dotnet test <dir>/<Project>.csproj -c Debug --no-build` | the test runner's exit status plus per-test results |
| List the generated classes | `dotnet test <dir>/<Project>.csproj -c Debug --no-build --list-tests` | the names to map to specifications |

`<Project>` is the project name the render used: the `--project-name` value when one was given,
otherwise the application name. Confirm it from the published `*.csproj` in the destination
before building, and never assume `<App>.csproj` when a `--project-name` was recorded.

Run build only after publication passed, tests only after the build of the same run. Re-render
after any model edit; never reuse an older destination. Give every build or test call an explicit
timeout and use the repository's own runner conventions; a timed-out or queued-out phase is
`not run: <reason>`, never a pass. For results worth keeping, ask the test runner for a results
file (for example `--logger "trx"` with a results directory under the probe folder) rather than parsing console text.

## 3. Reading results
- **Admission:** ok, or error codes classified with `renderability-gate.md`.
- **Publication:** written, unchanged and removed counts, plus `recovered`. `refused` when a managed file
  was modified, an unmanaged file sits on a planned path, a modified stale file would be removed,
  or the manifest identity changed (`drift-and-fallback-record.md`). Ok means `recovered: false` and
  `written + unchanged` equal to the artifact count (for a `--scope` render, the scope's artifacts: other
  scopes' files are preserved and never counted; compare against the receipt's `owners`/`sources` for the
  selected slices and the `orphaned` list). The receipt's `sources` map generated files back to
  specifications and slices.
- **Build:** Debug exit status. Classify each error location as `Customizations/`, managed or outside.
  An error in a managed file after a clean render is a renderer defect or an environment problem:
  report it, never patch the file.
- **Tests:** map per `.play` specification. A command specification renders as class
  `when_<snake>` in the namespace `<slice namespace>.when_<snake>`; a query assertion adds
  `_is_queried`, a read-model assertion `_is_projected`. The snake form of a name is not always
  what you would guess (`ReservingABerth` becomes `when_reserving_aberth`), so match against the
  listed classes, never predict them. Report four numbers apart: expected specifications,
  specifications with a generated class, passed, failed; list the specifications with no class.
  xUnit totals are not the measure.

## 4. Customizations (bounded)
Two kinds of hand-written code exist next to a generated base, and they differ in who writes
them and what they may do:

- **Seam adapters** (this skill, renderer role): code behind a seam the model names or the user
  asked for (an outbound adapter, a service registration, styles, packages). No business decisions.
- **Business gap-fill** (`slice-implementer`, only with the user's separate authorization, case B
  in `SKILL.md`): hand-written realization of scope Stage cannot render, with the model as the
  contract (`gap-fill-handoff.md`). It may live in `Customizations/` or a separate project. It
  never claims render admission: the rejected model stays rejected, and V5 covers the generated base only.

Allowed seams in Stage's managed publication: `Customizations/Program.cs` implementing the
partial hooks `ConfigureServices(WebApplicationBuilder)` and `ConfigureApplication(WebApplication)`,
other `Customizations/*.cs`, `Customizations/Dependencies.props`, `Customizations/styles.css`.
Stage never plans or reads this folder; a module or feature that would render into it fails
with `STAGE-CRATIS-005`. Contract and re-render rules: `cratis-stage-rendering-and-sandbox`
`references/rendered-application.md`. These seams exist only in a generated application (cases A
with an earlier base, and B); in case C there is no generated `Program.cs`.

Seam adapter procedure:
1. Name the seam and the model element it serves ("outbound mail adapter for the notification
   described on slice X"). No seam, no code.
2. Read, bounded: the hook signatures in the generated `Program.cs` and only the generated
   types the adapter touches.
3. Write the adapter behind a small interface; keep business decisions out of it. An adapter
   that calls an outside system follows `cratis-engineering-effect-boundaries`.
4. Build and test again, as separate calls.

Limits to state: services registered here are **not bound** to any Screenplay construct;
endpoints mapped here are anonymous; middleware order is fixed. A customization never makes a
rejected model renderable, and never turns a refusal into a V5 pass.

## 5. Done check (before reporting)
- Every modeled specification in the rendered scope has a generated class family that passed.
- No hand-written behaviour duplicates or extends a rendered slice.
- Seam adapters only serve named seams; authorized business gap-fill is in the fallback record; no managed file was edited.
- UI omissions are listed: modeled screens, forms, layouts, themes and persona views that the
  default composition does not render.
- V5 is reported as four lines, with the scope it covers stated. The runtime (`docker compose
  up`, the frontend build) is a further result: `not run` unless it was.

## 6. Stop and ask
Stop only when, after reading the slice and its specifications fully: a rule in a description
has no field, event or specification behind it; two specifications contradict; a specification
needs state nothing produces; a declared seam's contract conflicts with the model; or a
description states behaviour that a specification, mapping or constraint contradicts. Name the
slice, the element and what is missing. Everything else: proceed from what the model says.

## Interview batch for render delivery

1. **Which model, which accepted version?** (root, source identity from the lifecycle report).
2. **Application name** for `--name` (it is part of the application identity: keep it identical on every re-render).
3. **Destination.** Default `.ai-work/screenplay/<model-slug>/render-probe/<App>`; any other destination needs the user's intent.
4. **If admission fails:** stay in design mode, change the model for business reasons, or gap-fill by hand? (the three cases below).
5. **Seams:** outside systems, styles or packages the rendered app needs (`Customizations/`).
