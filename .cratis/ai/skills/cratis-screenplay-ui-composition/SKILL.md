---
name: cratis-screenplay-ui-composition
description: Compose the user interface of a Cratis Screenplay `.play` model — `layout` and responsive `arrangement`, screen/dialog templates, component data-context bindings, exposed template configuration, command forms and columns, navigation contributions, application navigation regions (topbar/sidebar, module inherit/extend/override), toolbar slice buttons, blueprint template references, sample data packs, hierarchical outlets/routes, toolbar dialogs, package/icon selection, interaction `behavior`/`on`/`uses` wiring, `ui profile` (root template, shell options, appearance, new screens default), `theme`, localized `$strings`, and `file` references. Use when declaring the application shell, reusable screen shapes, component/package configuration, command forms, navigation/toolbars, click or submit behavior, or theming. Do not use for a screen's own data and actions.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/SKILL.md -->

# UI composition in Screenplay

Read [preferred syntax](../cratis-screenplay-toolchain/references/preferred-syntax.md) when writing or reviewing `.play`.

Event Modeling's wireframe step is a first-class part of the language. The shell,
the reusable shapes inside it, the forms bound to commands, and the navigation
entries other modules contribute are all declared in the `.play` model.

⚠️ `file <Identifier>` is read as a **property** named `file`, not a directive —
the type-reference shape wins the tie. `file Invoices/Register.cs` is a directive;
`file Attachment` is a property. In a `trigger` body the directive always wins, so
a trigger value named `file` is written `@file`.

## Locate the model

Look first for the project's existing `.play` files: their folder is the model.
For a new model, check `Source/` or `src/`, else use `Screenplay/` at the repository
root. Never put a model under `.cratis/` or hand-copy it between repositories.
`cratis ai install` manages `.cratis/ai/`, never model files. Keep explanatory
Markdown in project documentation; `.play` is the single flow model.

## Product sources

Tool versions: toolchain `SKILL.md`. UI statements come from Screenplay
`screens.md`, `contributions.md`, `navigation.md`, `sample-data.md`, `forms.md`, `templates.md`, `ui-profile.md`,
`fixtures/screen-release-ui.play`, `layout-arrangement.md`, `interactions.md`, `theme.md`,
`internationalization.md` and `file-references.md` under `Documentation/screenplay/`. Scene
runtime sources: scaffold Scene tag, `Source/JavaScript/**`. This guidance does not verify
UI rendering.

Every construct on this page is deferred from the executable model with
information `PLAY0269`: it never blocks binding, and it is not evidence that a
target renders it.

The screens-release surface adds component data-context bindings, exposed template
configuration, package/icon catalogs, manual form columns, hierarchical outlets,
routes, toolbars and dialog destinations, each with a separate support level
(authoring accepted, executable admitted, runtime implemented). Discover the
installed shape through MCP `syntax-schema`; details:
`references/screen-composition-contract.md`.

Absent editor highlighting is never evidence that a construct is wrong: check `screenplay`, not the editor (see [references/screen-composition-contract.md](references/screen-composition-contract.md)).


## Compose in this order

Decide what every screen is for before deciding how any of it is laid out. Layout
work on a screen whose purpose is unsettled is rework.

1. **Level 1 for every screen in the flow first** (`cratis-screenplay-read-surface`:
   `data` plus `action`). Read the whole set, not one screen at a time.
2. **Review the flow before adding layout.** Walk it screen by screen as text
   (format in `references/flow-review.md`). When the host advertises the MCP-Apps
   extension for the `screenplay` MCP server, `visualize-model` (a `proposalId` or a `sketch`) can also show it on
   a board; that is optional, and the text review is enough. Do not
   use the Stage sandbox as the review: it renders only part of a screen, and
   nothing here establishes that Stage renders an authored screen faithfully.
   A behavior walk of what commands change is a separate, optional check owned by
   `cratis-stage-rendering-and-sandbox`.
3. **Add structure only to screens whose Level 1 is agreed**: template, sections,
   forms, `on`/`uses`. Use Level 3 inline code last, only where Level 2 cannot
   say it.
4. **Trace every field.** Each `form` `field` is a property of the form's command.
   A prefilled value comes from `populate` or `from`; a derived one from
   `compose using`. Each `summary` or `column` field is a property of the
   screen's read model. A value with no origin means the command or read model is
   missing it: fix the slice, never invent the value in the screen.
5. **Read before editing.** Open the existing screen (or `read-workspace` /
   `declaration-details` through the `screenplay` MCP server) and change only the directives that were asked
   for. Adding `layout` must not rewrite the Level-1 intent beneath it.
6. **Report each screen in two or three sentences**: what it shows and what the
   user can do. Not everyone reviewing can see a board.

Read `references/worked-flow.md` when a complete example is needed before composing a flow; it contains two Level-1 screens, their command, read model and form, with the field trace.

## `layout` — the application shell

One or more per application, declared at the top level; a `ui profile` selects the shell with `root template <Name>`.
A complete example:

```screenplay
layout AppShell
  category application
  topbar
  navigation contributes Navigation
  content
  footer

  arrangement flow
    column
      topbar height 56
      row
        navigation width 240
        content grow
      footer height 32
```

A slot is declared once by name, optionally marked
`contributes <ContributionPoint>` to make it a contribution target.

## `arrangement` — responsive `flow` or pixel-precise `freeform`

A `when width/height <class>` variant replaces the entire tree for that condition, never a partial override. Read [references/layout-arrangement.md](references/layout-arrangement.md) when writing `flow` containers, `freeform` variants or `place` lines.

## `screen template` and `dialog template`

Declared at module level, referenced by screens. Excerpt, inside a `module`:

```screenplay excerpt
screen template MasterDetail
  fits slot content
  sidebar
  main
  arrangement flow
    row gap 16
      sidebar width 280
      main grow

dialog template RegisterInvoiceDialog
  body
  actions
```

`fits slot <name>` says which slot of its parent the template fills. **A dialog
template is a screen template in everything but one respect: it declares no
`fits slot`, because a dialog occupies no slot of the structure it opens over** —
writing one is an error.

A screen fills a template by naming it and providing slot bodies; a dialog is
filled exactly the same way, because from the screen's side there is no
difference. See `cratis-screenplay-read-surface` for the directives that go inside.

## `form` — bound to a command

Excerpt, inside a `module`; the command, query and screen are declared in its
slices.

```screenplay excerpt
form RegisterInvoiceForm for RegisterInvoice
  populate via query GetInvoiceDefaults by customerId
  field invoiceNumber label "Invoice #"
  field customerId    from item
  field total         compose using TotalCalculator
  on submit navigate to InvoiceList
```

- At most one `populate` — `populate via query <Query> [by <param>]` or
  `populate from item`.
- `field <property>` takes at most one of `from <source>` or `compose using
  <Callback>`, plus an optional `label`.
- At most one `on submit navigate to <Screen> [by <param>]`, the one-line form.
  Any other `on` in a form body is a behavior attached to the form.

⚠️ **A form is discovered, not referenced.** It never appears in a screen's
directive tree the way a `table` or `summary` does — it is found by its
`for <Command>` binding wherever that command is invoked. A form sits at module
level, so it disambiguates by module rather than by feature or slice.

## `contribute`, toolbars, outlets and routes — navigation from elsewhere

Excerpt, inside a `module` or `feature`; `Navigation` is the contribution point
the layout above declares.

```screenplay excerpt
contribute to Navigation
  navigate to InvoiceList
  label "Invoices"
  order 20
```

All three directives are optional and appear at most once each.

**Resolution walks outward:** a contribution first looks for a
`contributes <ContributionPoint>` slot among its **own module's** templates, then
outward to the nearest enclosing structure that declares a matching point. Three
tiers exist — application-wide (the layout declares the point), cross-module, and
module-level.

`navigate to <Screen> by <param>` is **not** string interpolation; it reuses the
same typed navigate binding a screen action uses.

⚠️ A navigation contribution also carries `id`, `icon`, `presentation`, `group` and
`destination outlet <name> | dialog <DialogTemplate> | external "<route>"`; its `navigate to` takes
`route`, `outlet` and `parameter` lines, and an undeclared outlet or dialog target is `PLAY0654`
(`contributions.md`).

Requires Screenplay 4.133.0 or later (`screenplay --version`) for `PLAY0654` on an undeclared outlet or dialog target.

An explicit override for when nearest-enclosing is not the point you mean is
not in the language. The screens-release vector
(canonical `ScreenComposition` corpus v1) adds typed screen composition; a
renderer that admits it renders it and passes the browser run. Report a refused form as a
capability gap, never a flat-list fallback; Studio production proof is separate.

## The application shell: regions, slice buttons, blueprint references, sample data

The shell's navigation is declared in the `layout` (`navigation topbar` / `navigation sidebar`, with `logo` and nested
`item` lines) and each `module` states how it relates to a region: **inherit** (the default), **extend** (the
application's items plus its own) or **override** (its own only). Toolbars gain `button <id> title "<text>" slice
<Module>.<Feature>.<Slice>`, which opens a **State Change** slice as a dialog (a State View or Automation slice is
`PLAY0762`). A screen takes a template from a blueprint with `template blueprint <Blueprint> <Name> version "<range>"`,
and the `ui profile` selects the one active sample data pack (design and preview only, never in production).

```screenplay
layout AppShell
  category application
  content

  navigation sidebar
    item sales title "Sales" target Sales
      item orders title "Orders" target Sales.Orders

module Sales
  navigation sidebar extends
    item customers title "Customers" target Sales.Customers

  feature Orders
  feature Customers
```

Read [references/application-shell.md](references/application-shell.md) before writing any of it: the inherit/extend/override
choice, item target and slot, the module-only target rule (`PLAY0750`), buttons, feature and module toolbars (`toolbar` in a
`feature` or `module`, `confirm`), blueprint ranges, sample data, MCP editing
and the review checklist. `contribute to Navigation` stays the flat contribution; a new application uses regions.

## Interactions — what a click does

An interaction wires an `on <thing>` trigger to actions, inline or through named
`behavior` + `uses`. This is closed UI wiring, not scripting; validation stays on
the command. Trigger/action syntax is in [interaction example](references/interaction-example.md)
and [guarded interactions](references/guarded-interactions.md).

Guarded alternatives and `where` deprecation:
[guarded interactions](references/guarded-interactions.md).

Read [interaction example](references/interaction-example.md) when wiring a
named confirmation behavior and an inline refresh to the same screen.
Scalar/named `returns` bind and reference-execute (ESM v7).
Renderer generation/response limits: command-surface `references/generated-values-and-responses.md`.
Screenplay resolves response names in form submit and execute success (Requires Screenplay 4.148.0 or later);
own failure use is PLAY0685. No ESM change; Scene runtime binding remains downstream.
An existing success continuation does not imply a response contract.
Read [response continuations](references/response-continuations.md) for scope,
precedence, PLAY0485 and examples.

- Write the one-off case inline; name it with `behavior` when several places need
  the same wiring. A `uses` site must supply exactly the declared parameters
  (`PLAY0337`, `PLAY0338`).
- `on`/`uses` attach at every level — `layout`, `module`, `feature`, templates,
  `form`, `screen`, `section`, slot, `table` — and are **additive**: outer
  attachments run first unless a behavior declares `order`.
- `on success`/`on failure` belong only on actions that can fail (`execute`,
  `refresh`, `confirm`, `open dialog`, `raise`); `on result` only on `open dialog`.
  A continuation on `navigate`, `notify`, `set` or `close dialog` is an error
  (`PLAY0320`). Actions after an unconditional `navigate` are unreachable
  (`PLAY0339`).
- Unknown commands, screens, queries, dialogs and behaviors are warnings, like
  every other reference (`PLAY0330`–`PLAY0336`).
- **A declared `trigger` is not a UI event.** Use `on click`, not `trigger`, for a
  button. `on <Trigger>` observes an application trigger and `raise <Trigger>`
  fires one; that is where the two meet.

## `ui profile` and `theme`

A `ui profile` selects the application shell with `root template` (an own layout or application-scope screen template, or `blueprint <B> <Name>` with an optional `version` pin), its shell `option` lines, an `appearance` block (`color scheme`, `accent`), the template new screens start from (`new screens use template`), the theme and the `packages`, listed in override-priority order (a later package shadows an earlier one); `target platform` and `target size` accept any identifier, so a typo is silent. The legacy `layout <Name>` line inside a profile still reads but warns `PLAY0791`: write `root template`. `PLAY0790` to `PLAY0799` check the shell. Read [references/ui-profile-and-theme.md](references/ui-profile-and-theme.md) when writing a profile or theme, or fixing a shell diagnostic.

## Localized strings

Localized text lives in companion `.strings` files and is referenced unquoted as `$strings.<dotted.key>`. Read [references/localized-strings.md](references/localized-strings.md) when localizing messages, labels or titles.

## `file` — implementation or provenance, never a substitute

`file` is a backend attachment, a screen realization file or declaration provenance; use repository-relative paths (`PLAY0264`) and never let it replace the declaration. **Loaded is not run.** Read [references/file-references.md](references/file-references.md) when attaching or checking a `file`, or diagnosing `PLAY0430`–`PLAY0434`.

## Verify

- [ ] `screenplay <model> --warnaserror` (standalone) reports zero errors and zero
      warnings; `cratis screenplay validate --warnings-as-errors` says the same when its bundled
      compiler matches (versions: `cratis-screenplay-toolchain`
      `SKILL.md`).
- [ ] Every screen in the flow has Level 1 reviewed before any layout, and every
      form field and screen field traces to a command or read-model property.
- [ ] Every slot a `contribute` targets is declared `contributes <Point>` somewhere
      that encloses it.
- [ ] No `dialog template` declares `fits slot`.
- [ ] Each `ui profile` names its shell with `root template`, not the legacy `layout` line (`PLAY0791`); a blueprint shell is listed under `packages` and its `version` pin is deliberate.
- [ ] `packages` are ordered deliberately — later shadows earlier.
- [ ] Every theme a profile selects is `compatible with` one of its packages.
- [ ] `target platform` and `target size` values are spelled correctly; nothing
      checks them.
- [ ] User-visible text is `$strings.` where the application is localized.
- [ ] No `file` reference is doing work the declaration should be doing.
- [ ] Every click, submit and screen entry that does something has an `on` or
      `uses`; continuations sit only on actions that can fail.
- [ ] Navigation regions, slice buttons, blueprint references and sample data follow `references/application-shell.md`: each module's mode is deliberate, no module item targets another module, every button names a State Change slice, buttons shared by a feature's screens sit in a feature `toolbar` linked to its commands (`confirm` on destructive ones), every template has a `category`, and the active sample data pack is chosen once in the `ui profile`.
- [ ] Component bindings, exposed template values, manual form columns, toolbar items, outlets and routes all trace to typed data; no runtime fallback counts as delivery. Report authoring acceptance, executable admission and runtime
      implementation separately.

## Route near misses

- A screen's own data, actions and name resolution: `cratis-screenplay-read-surface`.
- Revision-checked MCP edits `cratis-screenplay-model-authoring` (`references/screen-authoring-workflow.md`).
- Application triggers and reactions: `cratis-screenplay-captures-and-reactions`.
- Reviewing the whole flow's design: `cratis-screenplay-model-review`.
- Deriving wireframes from the model (step 4): `cratis-screenplay-event-modeling`.
- Rendering/omissions: `cratis-stage-rendering-and-sandbox`, `cratis-screenplay-render-and-gap-fill`.
- Building the actual React application: `cratis-arc-react-page`, `cratis-components-styling`.

## Lineage

Read `references/provenance.md` when checking source attribution or adaptation permissions.
