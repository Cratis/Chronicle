<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/screen-composition-contract.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Screen composition contract

## Contents

- Support levels
- Data context and component bindings
- Guarded actions
- Exposed template configuration
- Forms and columns
- Packages, icons and template catalogs
- Routes, outlets, toolbars and dialogs
- Design-time generation results
- Canonical fixture ownership

This reference is the authoring checklist for the screens-release UI surface. It
records the contract an agent must look for in the installed Screenplay tool and
in peer runtime/render contracts; it is not a second language definition. The syntax
below was re-read at Screenplay v4.139.0 (`screens.md`, `forms.md`, `templates.md`,
`contributions.md`, `ui-profile.md` and `fixtures/screen-release-ui.play`); the shapes are released, not proposed. When a
syntax form below is not present in `syntax-schema` or does not compile with the
project's selected Screenplay version, report the support level instead of
hand-writing an alternate shape.

## Support levels

- **Authoring accepted**: the Screenplay parser, printer and MCP `syntax-schema`
  know the node. It may still be deferred from executable admission.
- **Executable admitted**: MCP executable diagnostics report no blocking errors for
  the construct in the current compiler.
- **Runtime implemented**: Stage, Studio or Scene consumes the authored construct
  without dropping it, silently falling back or replacing it with a default.

Keep those three lines separate in reports. Correct an old limitation only when
the package/version you are using proves the new level. Historical verified vector:
CLI 3.43.0 with its default `cratis/stage:4.52.0` and bundled Screenplay 4.127.0;
the executed vector renders on Scene 4.15.0. Packages published since are newer than
that vector and are not claimed browser-verified. Scene 4.17.0 was the earlier
source-checked runtime summary, not a new executed vector. Scene runtime capabilities
below were source-checked at the current scaffold's Scene tag; this does not
re-label the historical execution result. Current pins:
`cratis-screenplay-toolchain/references/versions.md`.
All three levels hold for the canonical `ScreenComposition` corpus on the historical vector:

- authoring and executable admission: MCP opens it with `sourceSuccess true`,
  `semanticSuccess true`, `executableReady true`, and the stdio MCP harness sends
  real proposal/apply requests, rejects a stale revision and preserves an
  authored comment on disk;
- runtime: render publishes, and the browser acceptance run on CLI 3.40.7 /
  `cratis/stage:4.51.1` passed 51 assertions with none blocked or pending
  (Cratis/Screenplay#605) - native command forms, required fields, scoped
  comments, deep links, stale-response handling and theme included. On CLI
  3.43.0 an agent-edited column was rendered, run and seen in the browser.

Guarded Close and double-click: Stage refused both with `STAGE-SCENE-ACTION-001` and
`STAGE-SCENE-INTERACTION-001` from Stage 4.51 through 4.57. Since Stage 4.65 they render and
run (public CLI 3.53.1 with `cratis/stage:4.73.1`, Cratis/Screenplay#671); the codes remain the
refusal for a guard Stage cannot evaluate, which is never emulated. Studio production Play
proof is a separate vector (Cratis/Screenplay#173). Refusing an unresolvable UI
profile and recovering a rendered application shipped in Cratis/cli#301. (History: on Stage 4.24.2 or
cratis 3.28.x renderers authored screens were reported as omitted/default composition; Stage 4.73.1
(CLI 3.53.1) renders navigation contributions and toolbars, and `CLI-RENDER-015`, which refused them, is retired.)

On CLI 3.53.1 (`cratis/stage:4.73.1`, corpus 4.137.0+) the browser run also passes a module-level navigation contribution, an unlisted
screen reached through the hierarchy, and a toolbar item that opens a dialog with its `presentation` (icon-only, hint)
honoured (Cratis/Screenplay#665, Cratis/Stage#336 and #337).

## Data context and component bindings

A component instance is a presentation of a typed Screenplay data context. The
released syntax (canonical fixture `fixtures/screen-release-ui.play`) is:

```screenplay excerpt
component <Package.Component> <instanceName>
  id "<stable-id>"
  context from query <Query>
  property <property.path> from component <stable-id>.<outputPath> null preserve
  property <property.path> from literal <value>
  property <property.path> = "literal"
  icon <IconName>
  presentation <key> "value"
  exposes <name> from component <stable-id>.<outputPath>
  outlet <name>
    ...nested screen directives...
  on click
    open dialog <DialogTemplate>
      with <name> from <binding>
```

A binding is `from query <Query>`, `from component <stableId>.<outputPath>` (quote the id when it has
punctuation) or `from literal <value>`, and may carry `mode oneWay|twoWay`, `null propagate|clear|preserve`
and `expected <Type>`. A component's `id` pins the stable instance id Studio and Stage use; without it the
authored name is the id. Malformed bindings are diagnostics, and the raw text is kept.

Rules for agents:

- Discover the exact node shape through `syntax-schema` before proposing it; do
  not infer JSON members from this prose.
- Every `context` and every `from` binding must trace to a screen `data`, command
  form value, route parameter, selected item, dialog input, `$context` value or an
  exposed template value. An untraced binding is a model defect.
- `property ... = "literal"` is static presentation configuration. A dynamic value
  uses `from` and keeps its source visible.
- `icon` names a package-qualified or profile-selected icon. Do not hard-code a
  renderer-specific icon library when the UI profile has not selected it.
- `presentation` keys are renderer/package inputs. They are authoring data, not a
  styling escape hatch; unknown keys must be surfaced by the consumer contract,
  not ignored.
- An `outlet` is a nested replacement surface; it can hold screen directives and
  can be targeted by navigation or a dialog route when the runtime contract
  supports it.

## Guarded actions

A label-headed screen action can select one command through authored-order
`when item.<path> ... execute ...` alternatives. First match wins; last
`otherwise execute` supplies a fallback, while `otherwise hidden` or omission
hides an unmatched action. With no item (loading/no selection), always hide it.
Explicit with-bindings, matching subject fields, form inputs and renderer input
fill the chosen command in that order. Authorization denial is not another
selector and never falls through. Refresh if a click-time check changes the
choice the user saw.

Missing fields are not null; == null matches explicit null only. Guards are
presentation offers, never access control. Downstream runtime support is separate
from parsing; do not publish an empty plain action as equivalent behavior.
The complete checked model and conditions are in
`cratis-screenplay-read-surface` `references/guarded-actions.md`, grounded in
Screenplay v4.125.0 `screens.md`. Historical CLI 3.41.0/Stage 4.51.3 checks reported
unsupported screen actions with STAGE-SCENE-ACTION-001; since Stage 4.65 a guard of the form
`item.<field>` compared with a literal is evaluated at runtime (Cratis/Screenplay#671).

## Exposed template configuration

Reusable templates can expose configuration and typed outlets:

```screenplay excerpt
screen template MasterDetail
  category workflow
  type master-detail
  exposes selectedInvoice InvoiceId
  outlet toolbar
  sidebar
  main
```

- `category` is used for template discovery and scope-aware catalogs. Suggested
  categories are stable product taxonomy, not renderer folders.
- `type` is the template kind a runtime/package recognizes.
- `exposes` declares values that slot bodies or nested components may bind to; a
  use site must provide or derive each required exposed value.
- `outlet` declares an addressable child surface. It is different from a static
  slot such as `sidebar` or `main`.

A template may also declare `content <slot>` (screen directives the template renders in that slot itself;
the slot must be declared, `PLAY0629`; templates may not nest in a cycle, `PLAY0656`), `display` and
`description` (what a picker shows), and `scopes application|module|feature|subfeature|slice|none`
(use outside its scopes is `PLAY0631`). Which of a template's components a screen may configure is a
top-level **exposure** (`exposure for <Owner>`, owner a layout or template, `PLAY0622`; `reexposes <Owner>`,
`PLAY0623`/`PLAY0624`), and a screen or template configures it with a top-level `instance <Screen>`
(`set <component>.<path> = <value>`, `items <component>.<path>` with `item <id>` lines; `PLAY0626` to
`PLAY0628`). Layouts take `category`, `type`, `exposes` and `outlet` too.

A module, feature or slice may select a default template for contained screens:

```screenplay
module Sales
  template MasterDetail
```

Treat this as inherited UI structure, not as copied directives. Check the
installed parser/printer supports the scoped `template` directive before using it.

## Forms and columns

A form remains bound to exactly one command. It may carry one `description` line (report-only),
`generation auto|manual` (Scene's form generation mode: generate fields from command metadata, or the
authored fields are the contract), a `layout` block (column, gap and placement geometry, independent of
`generation`) and the `columns` display-order hint. The screens-release additions make
its generated fields explicit when the automatic shape is not enough:

```screenplay excerpt
form RegisterInvoiceForm for RegisterInvoice
  columns auto
```

or:

```screenplay excerpt
form RegisterInvoiceForm for RegisterInvoice
  columns manual
    column invoiceNumber label "Invoice #"
    column total label "Total"
```

Use `auto` when the command shape is the form. Use `manual` when order, labels,
visibility or grouping matters. Every manual column is a command property; a
missing command property is fixed on the command, not by inventing a UI-only
field.

## Packages, icons and template catalogs

`ui profile` owns package priority and icon sets:

```screenplay excerpt
ui profile Desktop
  target platform web
  layout AppShell
  packages
    Product.DesignSystem
    Cratis.Components
  icons
    Product.Icons
```

- Package order is still override-priority order. Later packages shadow earlier
  ones only if the runtime contract says so; otherwise report an ambiguity.
- Component package metadata (kind, icon, scope, applicability and templates) is
  owned by the Scene/runtime package catalog. In the model, refer to it by package
  and component names; do not copy catalog JSON into `.play`.
- Template categories are scope-aware: application templates, module templates and
  package templates can share labels. Qualify where the tool reports ambiguity.

## Routes, outlets, toolbars and dialogs

Navigation and toolbar items target the same typed destination contract: screen,
named outlet, optional route parameters, and optionally a dialog.

```screenplay excerpt
toolbar InvoiceToolbar
  item register action RegisterInvoice
    label "Register"
    icon Add
  item details navigate to InvoiceDetails
    parameter invoiceId from component invoices.selectedItem.id
  item compose dialog InvoiceComposer
```

A toolbar item is `item <name> action <Command>`, `item <name> navigate to <Screen>` or
`item <name> dialog <DialogTemplate>`. Use a toolbar for reusable groups of actions; use inline `action`
when a single screen owns the button. A parameter that reads a selection is a typed
`from component <stableId>.<path>` binding (the bare `selected.invoiceId` form parses only as a legacy
data-context path). A dialog destination must state every input with a typed `with`/`parameter`
binding; a runtime default is not an authored contract.

Scene 4.21 runtime capabilities (source-checked at Scene v4.21.0, not a browser
run or a recheck at the newer scaffold pin). Authoring acceptance, executable
admission and runtime support stay separate — a Scene release is not a Stage
release. The current Stage scaffold pins a newer Scene release (Stage skill
`references/rendered-application.md`); never upgrade managed files by hand:

- **Hierarchy and routes.** `Screen.path` and `NavigationItem.hierarchy` carry
  module/feature paths, outermost first. Browser/hash history
  (`createBrowserSceneHistory`, `createHashSceneHistory`) supports deep links;
  dialogs are history entries closed with `closeDialog(result)`.
- **Bindings.** Inputs publish a `value` output through `useValueOutput`;
  command arguments resolve `component.<id>.<output>` through
  `resolveArgumentSource`. `useBoundQuery` reruns on argument changes, aborts
  prior runs and discards stale answers; clearing/failing removes old data.
- **Descriptors and packages.** Object descriptors carry nested `properties`
  and `open`. `ApprovedPackageSet`, `loadApprovedPackages` and
  `resolveApprovedPackageHost` align planning declarations with runtime bundles;
  executable imports require host approval.
- **Master/detail.** The default blueprint supplies `MasterDetail`.
- **Core toolbar.** `core:toolbar` holds `core:toolbarItem` entries. Action items
  announce `cratis.scene.command` with resolved parameters; Navigate/Dialog
  items execute through the navigation host (or announce a navigation event
  outside one). This is not a promise that the command executes without a host.
- **Nested outlets.** A `core:outlet` component declares a named nested outlet on a
  screen. A destination into it opens its screen there and keeps the declaring
  screen on the page; the opened screen may declare outlets of its own, so
  composition nests to any depth. A deep link to the innermost level composes
  every level above it, and back closes the innermost level only. An outlet
  declares the template types it accepts; a destination into a missing or
  incompatible outlet is a navigation diagnostic, not a fallback.
- **Toolbar buttons open destinations.** The Cratis Components toolbar button runs
  its attached interactions and opens its authored `destination` (a dialog or a
  slice screen) through the enclosing navigation host. Label, icon, text and
  presentation are properties of the button, so author them on the toolbar item
  rather than on a wrapper.
- **Settings pages.** `@cratis/scene.blueprint.components` ships a `SettingsPage`
  screen template: a module's settings as a generated command form with save and
  discard. Prefer it to a hand-built form for settings, and expose its command as
  template configuration.
- **Embedded hosts.** `WebViewPackageHost` renders a custom package set inside a
  webview (VS Code, Event Models) with no Studio dependency; the same package set
  must produce the same screens in the standalone host.

Whether a given Stage or CLI release renders each of these is a runtime level
claim: confirm it from the render receipt and diagnostics before reporting it.

## Design-time generation results

Design-time actions belong to component packages, not to the model and not to an
agent. Scene 4.15 (re-verified at the current scaffold Scene tag) declares them as data (`PackageDesignTimeMetadata`) and ships
their code in an optional design-time bundle; a runtime host that does not set
`loadDesignTime` loads none of it, so nothing a design-time action does can exist
only at runtime. The one Cratis action today is **Generate fields**
(`Cratis.Components.commandForm.generateFields`):

- it is visible only on a `commandForm` and enabled only when the command's
  metadata is known **and** the form has no `inputs` yet, so it never overwrites
  fields someone already laid out;
- it is deterministic: one field per command property, in command order,
  labeled from the property name, alternating between two columns;
- its result is a batch of `SetProperty` edits on the form's `inputs`, submitted
  to the host whole or not at all.

The model is where that result lives, not the callback. When you author a form as
an agent:

- Do not record, serialize or reproduce the package's generator code, and never
  invent a design-time hook in `.play`. Screenplay holds no callbacks.
- If the generated layout is the command shape, write `columns auto`; the
  renderer reaches the same result from command metadata.
- If someone changed it (order, labels, dropped fields), write `columns manual`
  with one `column <property> label "..."` per kept command property, in order.
  That is the durable representation of an edited Generate fields result, and it
  is why the action stays disabled on that form afterwards.
- Make the change through the MCP proposal/apply loop like any other edit, then
  render: a manual column that is not a command property is a model defect, not a
  generator defect.

Generated design-time output is evidence only when it is compared with the model:

- every screen, template, form, toolbar, component instance and outlet has an
  entry in the generated plan/report;
- every unsupported construct is reported as a blocking diagnostic, not silently
  dropped;
- the same model root, workspace export and package set are used by MCP, Studio,
  Stage and the runtime smoke test;
- package versions and image tags are printed with the check.

A screenshot, Storybook story, or rendered app that omits a modeled directive is
not a partial pass. It is either an unsupported construct with a diagnostic, or a
bug in the consumer.

## Canonical fixture ownership

Use the Screenplay-owned canonical corpus: the positive typed screen case lives
at `Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1/source/folder`
in the Screenplay repository. It demonstrates the released syntax for screens
with templates (`MasterDetail`), `data … via query` bindings, typed table
columns, `on row-click navigate to <Screen> by <param>`, sections, conditional
`when … execute … with … from …` actions and localized labels.
The AI corpus may link to that path and name the expected checks, but must not copy
the `.play` source into `.cratis/ai/` or maintain a second canonical application.
