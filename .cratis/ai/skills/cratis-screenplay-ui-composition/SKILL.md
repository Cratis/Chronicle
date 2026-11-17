---
name: cratis-screenplay-ui-composition
description: Use when declaring Screenplay shells, reusable templates, forms, navigation, interaction wiring, design systems, themes or free screens a slice adopts. Not for a screen's own query data and action contract; use cratis-screenplay-read-surface.
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

Look first for the project's existing `.play` files: their folder is the model;
code-only `Source/` or `src/` does not qualify. Otherwise use repository-root `Screenplay/` ([discovery/version gates](../cratis-screenplay-event-modeling/references/file-layout.md#locate-the-model)). Never put a model under `.cratis/` or hand-copy it between repositories;
`cratis ai install` manages `.cratis/ai/`, never model files. Keep explanatory Markdown in project documentation.

## Product sources

Tool versions: toolchain `SKILL.md`. UI statements come from Screenplay
`screens.md`, `outlets.md`, `contributions.md`, `navigation.md`, `sample-data.md`, `forms.md`, `templates.md`, `ui-profile.md`,
`fixtures/screen-release-ui.play`, `layout-arrangement.md`, `interactions.md`, `theme.md`,
`internationalization.md` and `file-references.md` under `Documentation/screenplay/`. Scene
runtime sources: scaffold Scene tag, `Source/JavaScript/**`. See
[Scene v4.82.0 runtime boundaries](references/scene-runtime-boundaries.md) for
exact query registration/submission, root-profile serialization and interaction
limits.

Every construct on this page is deferred from the executable model with
information `PLAY0269`: it never blocks binding, and it is not evidence that a
target renders it. `screenplay export --screens` admits the neutral members (data, actions, navigation, layout) as ESM v42; Scene and behavior wiring are not carried. A consumer of the `screenplay-ui/1` contract (`--format ui`) does not need Scene.

The screens-release surface adds component data-context bindings,
exposed template configuration, package/icon catalogs, manual form columns, hierarchical slots,
routes, toolbars and dialog destinations, each with a separate support level
(authoring accepted, executable admitted, runtime implemented). Discover the
installed shape through MCP `syntax-schema`; details:
[references/screen-composition-contract.md](references/screen-composition-contract.md).
Absent editor highlighting is never evidence that a construct is wrong: check `screenplay`, not the editor.


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

Read `references/worked-flow.md` for a complete example: two Level-1 screens, their command, read model and form, with the field trace.

## Free screens and Scene Design

A `screen` may be declared at the top level, in a module, a feature or a slice. A free screen states the data it needs (`data <Shape>[]`); a slice adopts it with `uses screen` and provides each need from a query. Read [references/free-screens.md](references/free-screens.md) before declaring, adopting or reviewing one: shapes, aliases, provisions, severities, design-only screens and their diagnostics. Read [references/scene-design.md](references/scene-design.md) when an agent designs screens through the Scene Design catalogue and operations or must refuse free styling.

## `layout` — the application shell

One or more per application, declared at the top level; a `ui profile` selects the shell with `root template <Name>`.
A complete example:

```screenplay
layout AppShell
  category application
  topbar
  navigation panel
  content page routed primary
  footer

  outlet Navigation of navigation in navigation

  arrangement flow
    column
      topbar height 56
      row
        navigation width 240
        content grow
      footer height 32
```

Requires Screenplay 6.0 or later; migrate a Screenplay 5 model first (`cratis-screenplay-modeling-lifecycle`).

A **slot** is a region content is placed in: each plain line, optionally followed by a role (`page`, `pane`,
`panel`), `routed [by <param>]`, `primary` and `optional`, in that order. An **outlet** is a list of items the
layout or a template offers: `outlet <name> of actions|filters|tabs|stats|navigation [for item|for screen] [in <slot>]`.
The Screenplay 5 forms (a region `outlet`, `destination outlet`, `contributes`, `navigation topbar|sidebar`, a
feature or module `toolbar`) are errors `PLAY0890`-`PLAY0896` that name the migration.

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
  category Dialog
  body
  footer
  outlet actions of actions in footer
    default cancel title "Cancel" runs close
    default register title "Register" emphasis primary runs submit
```

`fits slot <name>` says which slot of its parent the template fills. **A dialog
template is a screen template in everything but one respect: it declares no
`fits slot`, because a dialog occupies no slot of the structure it opens over** —
writing one is an error. Its buttons are an actions outlet (`runs close|submit` need no slice).

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
  field category
    choose from query AllCategories show name
  on submit navigate to InvoiceList
```

- At most one `populate` — `populate via query <Query> [by <param>]` or
  `populate from item`.
- `field <property>` takes at most one of `from <source>` or `compose using
  <Callback>`, plus an optional `label`.
- `choose from query <Query> show <field>` makes a field a picker over a list query's rows (`PLAY0913`).
  A field whose concept keys one list query's read model is a picker without it.
- At most one `on submit navigate to <Screen> [by <param>]`, the one-line form.
  Any other `on` in a form body is a behavior attached to the form.

⚠️ **A form is discovered, not referenced.** It never appears in a screen's
directive tree the way a `table` or `summary` does — it is found by its
`for <Command>` binding wherever that command is invoked. A form sits at module
level, so it disambiguates by module rather than by feature or slice.

## The application shell: slots, outlets, hooks, blueprint references, sample data

This covers navigation outlets (topbar/sidebar, inherit/extend/override), outlets of actions, filters, tabs and stats,
`contribute to`, hooks and placement, alongside blueprint references and sample data.

The layout declares its navigation outlets with `default` items and one `logo`; a module, feature or screen
**inherits** an outlet (the default), **extends** it (inherited items, then its own) or **overrides** it (its own only).

```screenplay
layout AppShell
  category application
  content page routed primary

  outlet sidebar of navigation
    default sales title "Sales" target Sales
      item orders title "Orders" target Sales.Orders

module Sales
  outlet sidebar extends
    item customers title "Customers" target Sales.Customers

  feature Orders
  feature Customers
```

- `contribute to <outlet>` adds an entry from any module or feature, next to the screens it belongs to
  (`navigate to <Screen> [by <param>]`, `label`, `order`, `destination slot|dialog|external`, or item lines of the
  outlet's kind). `by <param>` is a typed binding, never string interpolation.
- **Placement.** The template offers places, the slice says which it fits, the hook picks one. A slice's screen lists
  its fits in order of preference (`fits dialog, drawer, pane`). A **hook** - a `button` naming a State Change `slice` -
  opens it in its first fit the page has free; write `opens dialog|drawer|in slot <name>` only when this hook needs
  another place. `runs` issues the command without a form.
- `show <Screen> by <param> from <element>.selection` in a template slot shows another screen for the selected row.

Read [references/application-shell.md](references/application-shell.md) before writing any of it: slot roles, outlet
kinds and modes, `hide`/`before`/`after`, contribution resolution, the module-only target rule (`PLAY0750`), hooks
(`runs`, `opens`, `for item|for screen`, `enabled when`, `confirm`), `fits` and `show`, blueprint ranges, sample data,
MCP editing and the review checklist.

## Interactions — what a click does

An interaction wires an `on <thing>` trigger to actions, inline or through named
`behavior` + `uses`. This is closed UI wiring, not scripting; validation stays on
the command. Trigger/action syntax, including a named confirmation behavior with an inline
refresh, is in [interaction example](references/interaction-example.md)
and [guarded interactions](references/guarded-interactions.md).

Read [change alternatives](references/change-alternatives.md) when a component's
`on change` should choose an action list by the changed item, such as a kanban move.

State every plain and toolbar `action`'s subject: `for item` (the chosen record fills its
inputs) or `for screen` (no row does). Omitted, the compiler infers it from types, and a
create action on a list can bind the selected row's id. Read
[action subjects](references/action-subjects.md) when adding a screen action or when
PLAY0817-PLAY0821 appear.
Scalar/named `returns` bind and reference-execute (ESM v7).
Renderer generation/response limits: command-surface `references/generated-values-and-responses.md`.
Screenplay resolves response names in form submit and execute success (Requires Screenplay 4.148.0 or later);
own failure use is PLAY0685. No ESM change.
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

Extension components: [reference](references/extension-components.md).

## `design system`, `theme` and `ui profile`

The `design system` is the per-application contract; a `theme` implements it with values; a `ui profile` picks a theme, and `appearance` keeps only `color scheme` (`accent`, `corners`, `font`: `PLAY0929`). Read [references/design-system-and-themes.md](references/design-system-and-themes.md) first; components are the rendering contract (anatomy and link conventions).

A quoted theme name such as `theme "Scene Default Light"` needs Screenplay 4.227.0 (rules in the reference below).

A `ui profile` selects the application shell with `root template` (an own layout or application-scope screen template, or `blueprint <B> <Name>` with an optional `version` pin), its shell `option` lines, an `appearance` block (`color scheme`), the template new screens start from (`new screens use template`), the theme and the `packages`, listed in override-priority order (a later package shadows an earlier one); `target platform` and `target size` accept any identifier, so a typo is silent. The legacy `layout <Name>` line inside a profile still reads but warns `PLAY0791`: write `root template`. `PLAY0790` to `PLAY0799` check the shell. Read [references/ui-profile-and-theme.md](references/ui-profile-and-theme.md) when writing a profile or fixing a shell diagnostic.

## A deliverable app declares its identity, titles and labels

Symptom: a rendered app titled "Cratis Stage" whose buttons and dialogs show raw command names. The renderer
defaults to those when the model is silent, so declare the layout logo and title, a `ui profile` with `identity`,
a `title` per screen and navigation item, and a `label` on **every** `action`. Read
[references/deliverable-identity.md](references/deliverable-identity.md) for the full list and an example.

## Localized strings

Localized text lives in companion `.strings` files and is referenced unquoted as `$strings.<dotted.key>`. Read [references/localized-strings.md](references/localized-strings.md) when localizing messages, labels or titles.

## `file` — implementation or provenance, never a substitute

`file` is a backend attachment, a screen realization file or declaration provenance; use repository-relative paths (`PLAY0264`) and never let it replace the declaration. **Loaded is not run.** Read [references/file-references.md](references/file-references.md) when attaching or checking a `file`, or diagnosing `PLAY0430`–`PLAY0434`.

A screen can also keep `sketch`, `mockup` and `image` lines: authoring metadata, never part of the executable model. Read [references/screen-visuals.md](references/screen-visuals.md) for the lines and `PLAY0950`–`PLAY0952`.

## Verify

- [ ] `screenplay <model> --warnaserror` (standalone) reports zero errors and zero
      warnings; `cratis screenplay validate --warnings-as-errors` says the same when its bundled
      compiler matches (versions: `cratis-screenplay-toolchain`
      `SKILL.md`).
- [ ] Every screen in the flow has Level 1 reviewed before any layout, and every
      form field and screen field traces to a command or read-model property.
- [ ] Every outlet a `contribute to` or an `outlet <name> extends` names is declared by a layout or template;
      no Screenplay 5 form (`navigation topbar|sidebar`, `contributes`, feature `toolbar`, region `outlet`) remains.
- [ ] Every State Change screen says what it `fits`; a hook names a place only when the automatic choice is wrong.
- [ ] No `dialog template` declares `fits slot`.
- [ ] Every adopted free screen provides every need (no `PLAY1155`); unadopted screens are reported as design-only.
- [ ] Each `ui profile` names its shell with `root template`, not the legacy `layout` line (`PLAY0791`); a blueprint shell is listed under `packages` and its `version` pin is deliberate.
- [ ] `packages` are ordered deliberately — later shadows earlier.
- [ ] Every theme a profile selects is `compatible with` one of its packages.
- [ ] `target platform` and `target size` values are spelled correctly; nothing
      checks them.
- [ ] A deliverable app declares layout `logo ... title`, a `ui profile` with `option` lines and `identity`,
      a `title` per screen and a `label` on every `action`.
- [ ] User-visible text is `$strings.` where the application is localized.
- [ ] No `file` reference is doing work the declaration should be doing.
- [ ] Every click, submit and screen entry that does something has an `on` or
      `uses`; continuations sit only on actions that can fail.
- [ ] Navigation outlets, hooks, blueprint references and sample data follow `references/application-shell.md`: each level's outlet mode is deliberate, no module item targets another module, every hook names a State Change slice, buttons shared by a feature's screens extend the template's actions outlet once (`confirm` on destructive ones), every template has a `category`, and the active sample data pack is chosen once in the `ui profile`.
- [ ] Component bindings, exposed template values, manual form columns, toolbar items, outlet items and routes all trace to typed data; no runtime fallback counts as delivery. Report authoring acceptance, executable admission and runtime
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
