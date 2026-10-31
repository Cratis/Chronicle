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
- Edit the application shell
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
accepted model. Structural AST edits use `CanonicalizeTouchedDocuments`; literal
or mapping-only AST changes may use `PreserveTrivia`. For hand-written whole
source documents, use `propose-source` with `PreserveExactSource` so structural
edits keep supplied comments and layout (including line endings and BOM policy).
Choose canonical printing only when that output is wanted.

Requires Screenplay 4.158.0 or later (`screenplay --version`) for exact structural source edits; older servers refuse them.

## 3. Review and apply with revision checks

Before applying:

- `read-proposal view=changes` and document before/after views;
- report the proposal's `summary`, not an invented count-based recap;
- inspect `authoringDiagnosticSeverities.errors` and `codes`, not merely a nonzero
  `authoringDiagnosticCount`: PLAY0288 alone reports canonical printing even
  without comment loss. Proposal checks and `after.readiness` source diagnostics
  are different counts; page `read-proposal view=diagnostics`;
- keep checking `introducedExecutableErrors` and `droppedCommentCount`;
- compare every screen/form/toolbar binding against field lineage;
- reject proposals that silently drop a requested screen directive or package
  setting;
- apply only with the before revisions returned by `open-workspace` / proposal.

Requires Screenplay 4.156.0 or later (`screenplay --version`) for proposal summaries and severity counts; older servers lack these fields.

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

`--check` plans exactly like a render and reports per path what a real
render would write, delete, leave unchanged or refuse, without writing anything. It
exits `0` when the destination is current, `6` (`changes_pending`) when a render
would change it and `5` when planning is refused. The receipt lists
for every path the semantic declarations it realizes (`sources`: id, kind and a
`Module/Feature/Slice/Name` address) and a `bySource` view mapping each declaration
to its files. After an MCP edit, use `bySource` to confirm the edited screen or
binding changed exactly the files you expect, and `semantic-diff` (MCP:
`before` and `after` as `{ "workspace": "active" }`, `{ "path": "<folder>" }`
or `{ "workspaceJson": "<export>" }`) to compare the model before and after the
edit.

Requires cratis CLI 3.44.0 or later (`cratis --version`) for `cratis render --check`.

For screens-release parity, the smoke check must assert that every authored
screen, template, component, form, toolbar, outlet, route and package/icon use is
represented in the generated plan or runtime diagnostics. A consumer that cannot
implement one reports a blocking diagnostic. Silent fallback to the default screen
composition is a failure.

## Edit the application shell

Navigation regions, toolbar buttons, blueprint template references and sample data are typed nodes and follow the same
open, read, propose, review, apply, reopen loop; the syntax and rules are in
`cratis-screenplay-ui-composition` (`references/application-shell.md`). The vectors to replay against are the
`screen-shell-navigation-positive` source case and its rejection vectors in `Cratis.Screenplay.CanonicalCorpus`.

1. `read-ast` with `kind` `NavigationRegionSyntax`, `NavigationItemSyntax`, `ToolbarButtonSyntax`,
   `TemplateAssignmentSyntax`, `ScreenTemplateReferenceSyntax` or `SampleDataPackSyntax`, `includeContent: true`.
   Select the occurrence by its parent: a region belongs to the `LayoutSyntax` or a `ModuleSyntax`, and the same item id
   can appear in several regions.
2. `propose-ast` with `replace` (copy the returned node, change `mode`, `items`, `slice`, `version` or a row value),
   `add` to a member the node kind declares (`navigation`, `items`, `children`, `buttons`) or `remove`. A member that
   holds nothing is not in the node JSON; `add` still accepts it.
3. Read the proposal's diagnostics before applying. A refusal such as `PLAY0750` (a module item targeting another
   module), `PLAY0762` (a button on a State View slice), `PLAY0722` (a blueprint no profile lists) or `PLAY0714` (an
   unknown active pack) is the answer; change the proposal, not the rule.
4. `apply` with both revisions. A `StaleRevision` means the model moved: reopen, re-read the nodes and propose again.
5. Switching a module from inherit to override copies the application's items; retarget each copy to the module's own
   features first or the proposal is refused.

### Feature and module toolbars

A feature (or module) toolbar is a `ScreenToolbarSyntax` in the `toolbars` member of its `FeatureSyntax` (or `ModuleSyntax`);
`syntax-schema` for `FeatureSyntax` lists `toolbars`. `add` a `ScreenToolbarSyntax` with `name`, `items` and `buttons` to the
feature, and `replace` a `ToolbarButtonSyntax` or `ToolbarItemSyntax` to set its `confirm` text; `remove` takes a button away.
Read the proposal's diagnostics: a warning says an `item ... action` names a command no slice of that feature (or module)
declares. The syntax, the `confirm` rule and when to prefer the feature toolbar are in `cratis-screenplay-ui-composition`
(`references/application-shell.md`, "Feature and module toolbars"; Screenplay 4.238.0 or later).

### The profile's shell

`ui-profiles` (Screenplay 4.201.0 or later) reads each `ui profile`'s root template, shell options, appearance, new screens default and
shell diagnostics, and lists the own templates that could be the root template (`applicableRootTemplates`) or the new screens
default (`applicableNewScreenTemplates`); pick from those instead of guessing. Edit `RootTemplateSyntax` (`name`, `blueprint`,
`version`), `ShellOptionSyntax` (`add` to the root's `options`; `syntaxKind` is `Word`, `Text`, `Number` or `Flag`),
`ProfileAppearanceSyntax` (`colorScheme`, `accent`) and `NewScreenTemplateSyntax` with `propose-ast`. A refusal is the answer:
`PLAY0790` (a template nothing declares), `PLAY0792` (a blueprint the profile does not list), `PLAY0794` (a template that cannot
be the shell), `PLAY0795` (no `content` slot), `PLAY0797` (a bad color scheme, accent, corners or identity value), `PLAY0799` (a new screens default that
cannot start a screen). `PLAY0796` (an option the shell does not expose) and `PLAY0791` (the legacy `layout` line) are warnings:
the proposal is accepted and `ui-profiles` shows them. Replay against the `ui-profile-root-positive` vector and its rejection
vectors (`ui-profile-root-unknown`, `ui-profile-root-not-a-shell`, `ui-profile-root-invalid`) in `Cratis.Screenplay.CanonicalCorpus`.

Requires Screenplay 4.188.0 or later (`screenplay --version`) to `add` the first region, button or sub-item to a member
the node JSON omits while empty; an older MCP server answers "Unknown typed syntax member".

## 6. Use Screenplay-owned fixtures

The canonical multi-file application belongs to the Screenplay/conformance corpus,
not to the AI corpus. Link to the fixture path and run its published checks; do not
copy those `.play` files into `.cratis/ai/` or into a skill reference as a
maintained duplicate.

Executable checks against the corpus:

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
  remains on disk. A passing run shows `executableReady true`, stale apply `StaleRevision` and the comment preserved;
- render (`cratis render <corpus-folder> --name ScreenComposition --destination
  <dir> -o json`) publishes the corpus;
- guarded Close and double-click: Stage renders and runs them; the codes
  `STAGE-SCENE-ACTION-001` and `STAGE-SCENE-INTERACTION-001` remain for a guard Stage
  cannot evaluate. Studio production Play proof is a separate vector. UI-profile
  rejection and render-then-recover are covered by the CLI.

## Replay the workflow in CI without a Screenplay checkout

`screens-smoke.sh <work-folder>` in the AI repository's `screens-smoke` verification folder installs the pinned public `cratis` CLI,
materializes the published `Cratis.Screenplay.CanonicalCorpus` folder source (so the corpus stays Screenplay-owned and
is never copied), replays the MCP transcript with the revision-checked column edit, renders the edited folder and
requires the column in the generated `scene.json`. The pins live in `screens-smoke.json`; the corpus package records the
expected results. The corpus verification job runs it on every
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
