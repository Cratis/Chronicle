<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/screen-authoring-workflow.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# End-to-end screen authoring workflow

## Contents

- Inputs to keep identical
- 1. Open and discover through MCP
- 2. Propose the multi-file application change
- 3. Review and apply with revision checks
- 4. Validate authoring and executable readiness
- 5. Run/render using the same configuration
- 6. Use Screenplay-owned fixtures
- Replay the workflow in CI without a Screenplay checkout
- Edit, render, run and see it in the browser
- Transcript checklist

Use this workflow when an agent must create or iteratively edit the canonical
multi-file Screenplay application and then prove the same source reaches the
runtime/render path. It relies on the existing MCP discovery/proposal/apply loop;
it does not define a parallel persistence or editing API.

## Inputs to keep identical

Record these once and pass the same values to every step:

- model root, application name and source identity;
- selected Screenplay tool (`screenplay mcp` or `cratis screenplay mcp`) and its
  reported version/tool schemas;
- package/catalog source and versions for component packages and icons;
- Stage/CLI render destination and runtime target;
- workspace export path if a host uses `export-workspace` rather than source files.

If any step uses a different root, workspace export, package set or application
name, it is not parity evidence.

## 1. Open and discover through MCP

1. Start the MCP with a fixed root and send `initialize` plus
   `notifications/initialized`.
2. Call `tools/list` and keep the schemas. Do not hard-code the screen node shape.
3. `open-workspace` and keep both source and catalog revisions.
4. Use `describe-application`, `read-workspace view=diagnostics`,
   `search-declarations`, `declaration-details` and `dependencies` to locate the
   target screen, template, form, toolbar and package references.
5. Use `syntax-schema` for every node family you will write: screen directives,
   forms, template configuration, package/icon declarations, toolbar/directive
   nodes and navigation/dialog destinations.

When the node does not exist in the schema, stop at an edit request that names the
missing authoring capability; do not invent text syntax.

## 2. Propose the multi-file application change

Use one coherent `propose-ast` transaction for the slice of UI being changed.
For a new canonical application this usually creates or changes:

- the application/barrel document and imports;
- module/feature/slice documents;
- read models and queries that supply screen data;
- screen declarations with Level 1 data/actions;
- templates, components, forms, toolbar items, outlets and navigation bindings;
- localized `$strings` keys referenced by labels.

Use `referencePolicy: "Safe"` unless unresolved debt is explicitly part of the
accepted model. Structural edits use `CanonicalizeTouchedDocuments`; literal or
mapping-only changes may use `PreserveTrivia`.

## 3. Review and apply with revision checks

Before applying:

- `read-proposal view=changes` and document before/after views;
- check introduced diagnostics and `droppedCommentCount`;
- compare every screen/form/toolbar binding against field lineage;
- reject proposals that silently drop a requested screen directive or package
  setting;
- apply only with the before revisions returned by `open-workspace` / proposal.

After `apply`, discard old handles and revisions. Reopen/read the workspace and
verify the source and identity state changed as expected.

## 4. Validate authoring and executable readiness

Run the whole model folder, not a single file:

```shell
cratis screenplay validate <model-folder> --warnings-as-errors
```

Then read executable diagnostics through MCP. Report separately:

- authoring validation (parsed plus source rules);
- executable readiness (MCP `executableReady` and blocking diagnostics);
- unsupported design-only UI (`PLAY0269` or later equivalent) without treating it
  as a source error.

## 5. Run/render using the same configuration

Rendering and runtime smoke checks use the same root or workspace export from the
MCP step:

```shell
cratis render <model-folder> --name <ApplicationName> --destination <render-dir> -o json
```

When a workspace envelope is the source of truth for the host, use the Stage/CLI
contract for `--workspace` and keep its application identity equal to the source
name. Build/tests/runtime checks are then the V5 steps owned by
`cratis-screenplay-render-and-gap-fill` and `cratis-stage-rendering-and-sandbox`.

Before re-rendering an edited model over an earlier render, preview it:

```shell
cratis render <model-folder> --name <ApplicationName> --destination <render-dir> --check -o json
```

`--check` (CLI 3.44.0) plans exactly like a render and reports per path what a real
render would write, delete, leave unchanged or refuse, without writing anything. It
exits `0` when the destination is current, `6` (`changes_pending`) when a render
would change it and `5` when planning is refused. Since CLI 3.45.0 the receipt lists
for every path the semantic declarations it realizes (`sources`: id, kind and a
`Module/Feature/Slice/Name` address) and a `bySource` view mapping each declaration
to its files. After an MCP edit, use `bySource` to confirm the edited screen or
binding changed exactly the files you expect, and `semantic-diff` (MCP, Screenplay
4.131.0: `before` and `after` as `{ "workspace": "active" }`, `{ "path": "<folder>" }`
or `{ "workspaceJson": "<export>" }`) to compare the model before and after the
edit. These are newer than the verified vector: report them as published, not as
executed there.

For screens-release parity, the smoke check must assert that every authored
screen, template, component, form, toolbar, outlet, route and package/icon use is
represented in the generated plan or runtime diagnostics. A consumer that cannot
implement one reports a blocking diagnostic. Silent fallback to the default screen
composition is a failure.

## 6. Use Screenplay-owned fixtures

The canonical multi-file application belongs to the Screenplay/conformance corpus,
not to the AI corpus. Link to the fixture path and run its published checks; do not
copy those `.play` files into `.cratis/ai/` or into a skill reference as a
maintained duplicate.

Historical verified public vector and executable checks:

- executed toolchain: public CLI 3.43.0 (`23e5a24`), whose default runtime was
  `cratis/stage:4.52.0` and which bundled Screenplay 4.127.0. Published since,
  and not verified by this vector: Scene 4.17.0, Screenplay 4.133.1, Stage 4.64.0,
  previous CLI 3.46.1 (bundling Screenplay 4.130.0, then-default Stage 4.52.0) and
  Studio 0.152.2. Current pins: `cratis-screenplay-toolchain/references/versions.md`.
  Compiler claims from this `cratis` probe are 4.127.0 claims, not current
  bundled-compiler claims;
- fixture root: `Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1/source/folder`
  in the Screenplay repository;
- Screenplay repo tool (available checkout): `dotnet run --project Source/DotNET/Tool
  -- <corpus-folder> --warnaserror` → exit `0`, `10 file(s) compiled - 0 error(s),
  0 warning(s)`;
- stdio MCP protocol harness: the AI repository carries `screenplay-mcp-transcript.ts`;
  run it with packaged `cratis` on `PATH`, `--corpus <corpus-folder>` and
  `--scratch <scratch-folder>`. It sends `initialize`, `tools/list`,
  `open-workspace`, `read-workspace`, `propose-source`, `read-proposal`, stale
  `apply`, real `apply` and `open-workspace` again. It uses a scratch copy,
  changes `application.play` with a comment-preservation probe, fails closed on a
  stale revision, applies with the matching revision and verifies the comment
  remains on disk. On CLI 3.43.0: 9 requests and 9 responses, 37 tools,
  `executableReady true`, stale apply `StaleRevision`, comment preserved;
- render (`cratis render <corpus-folder> --name ScreenComposition --destination
  <dir> -o json`) with CLI 3.43.0 publishes the corpus; the browser acceptance
  run on CLI 3.40.7 passed 51 assertions (Cratis/Screenplay#605);
- guarded Close and double-click: refused with `STAGE-SCENE-ACTION-001` and
  `STAGE-SCENE-INTERACTION-001` on this historical vector, rendered and run since
  Stage 4.65 (live browser run on CLI 3.53.1 with `cratis/stage:4.73.1`,
  Cratis/Screenplay#671); the codes remain for a guard Stage cannot evaluate. Studio
  production Play proof is a separate vector (Cratis/Screenplay#173). UI-profile
  rejection and render-then-recover shipped in Cratis/cli#301.

## Replay the workflow in CI without a Screenplay checkout

`screens-smoke.sh <work-folder>` in the AI repository's `screens-smoke` verification folder installs the pinned public `cratis` CLI,
materializes the published `Cratis.Screenplay.CanonicalCorpus` folder source (so the corpus stays Screenplay-owned and
is never copied), replays the MCP transcript with the revision-checked column edit, renders the edited folder and
requires the column in the generated `scene.json`. The pins live in `screens-smoke.json` and follow the verified
vector above; the corpus package is the one that records that vector. The corpus verification job runs it on every
change. With `SCREENS_SMOKE_BROWSER=1` (the CI job sets it) it also runs the edited folder with `cratis run`
and checks the edited column and a created row in Chromium with the pinned Playwright, so the browser step below runs in
corpus CI too; locally it needs Docker.

## Edit, render, run and see it in the browser

The loop above proves an agent can change the model. This proves the change
reaches a running application, end to end, on the installed public CLI:

1. Edit through MCP: `screenplay-mcp-transcript.ts --edit work-item-id-column
   --corpus <corpus-folder> --scratch <work>/model` adds `column workItemId label
   "Work item id"` to the `WorkItemList` table in the same revision-checked
   proposal as the comment probe, and requires the reopened workspace to be
   executable-ready.
2. Render the edited folder: `cratis render <work>/model --name ScreenComposition
   --destination <work>/app -o json`, then confirm `scene.json` has the new
   column (`"label":"Work item id","property":"workItemId"`).
3. Run and look: `screenplay-edited-app-browser.ts --model <work>/model` starts
   `cratis run` on that same folder, creates a work item through the generated
   command endpoint, opens `#/WorkItemList` in headless Chromium and requires the
   `Work item id` column header and the new item's id in the table. It needs
   Docker and a resolvable `playwright` (`SCREENPLAY_PLAYWRIGHT_NODE_PATH`); exit
   `0` verified, `1` edit not visible, `2` could not run.

Executed on public CLI 3.43.0 (`23e5a24`, Stage 4.52.0, bundled Screenplay
4.127.0): 9/9 MCP requests with the column on disk, render `published` with the
column in `scene.json`, and all five browser checks passed against
`cratis/stage:4.52.0` (also passed earlier on CLI 3.41.0 / `cratis/stage:4.51.3`).

Report a corpus the released compiler, MCP server or renderer refuses as a
capability/version-pairing gap with the exact diagnostics; never soften it to a
pass.

When the fixture is not yet published, report the parity checks as blocked by the
fixture contract and continue verifying the independently authored guidance and
routing in this repository.

## Transcript checklist

An MCP transcript is acceptable evidence only when it contains:

- `tools/list` with the version/tool schemas used;
- `open-workspace` revisions;
- discovery calls that found the target declarations;
- `syntax-schema` calls for each new UI node family;
- proposal id, `read-proposal` diagnostics, comment-loss result and before/after
  revisions;
- `apply` with matching revisions;
- post-apply `read-workspace` diagnostics;
- the same root/workspace passed to validate/render/runtime checks.

Store the transcript in `.ai-work/` while working. Do not commit it.
