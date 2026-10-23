<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/application-shell.md -->
# The application shell: navigation regions, toolbar buttons, template references and sample data

## Contents

- Default shell
- One complete shell
- Navigation regions: inherit, extend or override
- Toolbar buttons that invoke a State Change slice
- Feature and module toolbars
- Template references, categories and blueprints
- Sample data packs
- Shapes the compiler refuses
- Editing the shell through MCP
- Render and verify
- Review checklist

Requires Screenplay 4.180.0 or later (`screenplay --version`) for regions, slice buttons, blueprint references and sample
data packs, and Cratis CLI 3.67.0 or later (`cratis --version`) for `cratis render` and `cratis run` to carry them. An
older compiler answers `PLAY0001`, `PLAY0022` or `PLAY0103` for the lines below; that is the version, not the model.

Every construct here is authoring syntax. The executable model defers it (`PLAY0269`), Stage plans it from the syntax
tree, and none of it changes a backend. The Scene designer, the Stage runtime and Studio read the same lines, so the
Screenplay is the one place to write them.

## Default shell

Requires Stage 4.189.3 or later (the CLI that bundles it) for a rendered frontend to get a default application shell without a
`layout` or `ui profile`. Declare a `layout` and a `ui profile` only to customize that shell
(navigation, logo, templates, sample data), as below.

## One complete shell

```screenplay
layout AppShell
  category application
  topbar
  side
  content

  navigation topbar
    logo image "/assets/acme.svg" title "Acme"
    item sales title "Sales" icon receipt target Sales
    item support title "Support" target Support slot content
  navigation sidebar
    item sales title "Sales" icon receipt target Sales
      item orders title "Orders" target Sales.Orders
      item returns title "Returns" target Sales.Orders.Returns slot side
    item support title "Support" target Support

ui profile Desktop
  target platform web
  root template AppShell

  packages
    core
    Crm

  sample data
    active NordicDemo
    shape Customer maps to CustomerRow

sample data NordicDemo
  title "CRM · Nordic SMB"
  locale nb-NO
  shape Customer
    row
      name = "Fjordline AS"
      revenue = 125000.5
    row
      name = "Nordlys AS"
      revenue = 98000

module Sales
  navigation sidebar extends
    item customers title "Customers" icon people target Sales.Customers
  navigation topbar inherits

  template blueprint Crm Workspace version "^1.4"

  feature Orders
    feature Returns

    slice StateChange PlaceOrder
      command PlaceOrder
        orderId Uuid identifier
        reason String
        produces OrderPlaced
          for orderId
          reason = reason
      event OrderPlaced
        reason String

    slice StateView OrderList
      readmodel OrderRow
        orderId Uuid
        title String

      screen OrderList
        template blueprint Crm CustomerDetail version "~1.4.2"
          body
            title "Orders"
            toolbar actions
              button place title "New order" slice Sales.Orders.PlaceOrder emphasis primary icon plus

  feature Customers
    slice StateView CustomerList
      readmodel CustomerRow
        name String
        revenue Decimal

module Support
  navigation sidebar overrides
    item tickets title "Tickets" target Support.Tickets
  navigation topbar inherits

  feature Tickets
    slice StateView TicketList
      readmodel TicketRow
        title String

module Reports
  feature Monthly
    slice StateView MonthlyTotals
      readmodel Totals
        total Decimal
```

This is the canonical shell vector (`screen-shell-navigation-positive` in `Cratis.Screenplay.CanonicalCorpus`),
rendered by Stage with its sidebar, topbar, slice dialog and deep links. Start from it; do not invent the syntax.

## Navigation regions: inherit, extend or override

The `layout` declares the application's `navigation topbar` and `navigation sidebar`; a `module` states how it relates to
each region it changes. A region a module does not mention **inherits** the application's items.

| Module says | Items the module shows | Choose it when |
| --- | --- | --- |
| nothing, or `navigation <region> inherits` | The application's items | The module needs no navigation of its own (the default) |
| `navigation <region> extends` | The application's items, then the module's own | The module adds items (a sales sidebar that also lists Customers) and keeps the application's |
| `navigation <region> overrides` | The module's own items only | The module replaces the whole region (a support area with its own sidebar) |

- Each region has its own mode: a module can extend the sidebar and inherit the topbar.
- The application has no mode, and the layout never says `inherits`, `extends` or `overrides` (`PLAY0745`).
- `logo image "<path>" title "<text>"` belongs to the layout's region only; a module that declares one is `PLAY0744`.
- An item is `item <id> title "<text>" [icon <name>] [target <Module>[.<Feature>]] [slot <name>]`. The id is unique in
  its region, and an extending module never repeats an application id (`PLAY0747`). The title is a quoted string or
  `$strings.<key>`.
- `target` names a module (the module's first feature) or a feature path. An item with no target needs sub-items, and a
  target the document does not declare is `PLAY0748`. A module with no feature warns (`PLAY0751`).
- `slot` names a layout slot the target renders into; without it the target renders into the primary content slot. A slot
  no layout declares is `PLAY0749`.
- **Hierarchy is sidebar only.** Top-level items target modules and their sub-items target features of that module. A
  topbar item with sub-items is `PLAY0746`.
- **A module's own items may only target that module** (`PLAY0750`). To link elsewhere, put the item in the layout. This
  matters when switching a module from inherit to override: the copy of the application's items must be retargeted to
  the module's own features, or it is refused.
- An `overrides` region with no items leaves the region empty and warns (`PLAY0752`).

⚠️ `navigation topbar|sidebar` is a **region**; `navigation contributes Navigation` in a layout is a **slot**, and
`contribute to Navigation` in a module is a flat **contribution** (see `SKILL.md`). They are independent: a new
application models its navigation with regions and does not also generate contributions for the same entries.

## Toolbar buttons that invoke a State Change slice

A `button` is a toolbar entry that opens a State Change slice as a dialog over the page; the button needs no command of its
own.

- `button <id> title "<text>" [slice <Module>.<Feature>.<Slice>] [emphasis primary|secondary|quiet] [icon <name>]`.
- `slice` is a dotted path or any unambiguous suffix (`PLAY0764` when it matches several). It must name a slice the
  document declares (`PLAY0761`) and a **State Change** slice. A State View, Automation or Translate slice is `PLAY0762`;
  a button invokes State Change slices only.
- A button without a `slice` is legal while designing and warns (`PLAY0763`); never ship one.
- A button id is unique within the toolbar and shared with `item` names (`PLAY0765`).
- `item` entries (`action`, `navigate to`, `dialog`) stay valid beside buttons. Use `button` to start a slice, and
  `item ... navigate to` to go to a screen.

### Application-level buttons

A layout's `navigation topbar` region takes the same `button` line once for the whole application, so an action stays at hand
whichever page the user is on. An indented `authorize <Policy>` line under a button limits who sees it. A module's
`navigation topbar extends|overrides|inherits` governs the buttons as it does the items. A button in `navigation sidebar` is
`PLAY0741`. The slice rules above (`PLAY0761`-`PLAY0765`) apply unchanged (decision 0098).

```text
layout AppShell
  category application
  topbar
  navigation contributes Navigation
  content
  navigation topbar
    item shop title "Shop" target Shop
    button newProduct title "New product" slice Merchandising.Products.DraftProduct emphasis primary icon plus
      authorize CanCurateCatalog
```

⚠️ When a screen uses a `template`, put the `toolbar` inside a slot body (`body` above). Stage refuses a screen that
mixes a top-level `template` directive with other top-level directives.

## Feature and module toolbars

Requires Screenplay 4.238.0 or later (`screenplay --version`); an older compiler refuses `toolbar` in a `feature` or `module`.

A template declares a `toolbar` slot. When the same actions belong to every screen of a feature, write the toolbar once on the
`feature` (or on the `module` for every feature below it) instead of repeating it on each screen: the entries are contributed to
the `toolbar` slot of every screen beneath it, sub features included, after the screen's own entries (the nearer toolbar comes
later), and each entry runs one command of the feature. A screen that needs its own buttons keeps its own `toolbar`.

```text
module Merchandising
  feature Publication
    toolbar actions
      button approve title "Approve product" slice Merchandising.Publication.ApproveProductForSale emphasis primary icon check
      button withdraw title "Withdraw product" slice Merchandising.Publication.WithdrawProduct emphasis quiet icon ban
        confirm "Take this product off sale?"
      item archive action ArchiveProduct
        label "Archive"
        icon archive
```

- The lines are the ones of a screen toolbar: `item <id> action <Command>` and `button <id> title "<text>" slice <Slice>`, with
  the `PLAY0761`-`PLAY0765` rules above.
- `confirm "<text>"` (or a `$strings.<key>` token) is an optional child line of an `item` and of a `button`: the user confirms
  before the command runs or before the button's dialog opens. Use it for destructive or hard-to-undo commands, not for every entry.
- An `item ... action <Command>` must name a command that a slice of the feature (or module) declares; otherwise the compiler
  warns that the command is not the feature's. Fix the name or move the toolbar to the feature that owns the command; never silence the warning.
- Choose the feature toolbar when the buttons act on the feature as a whole (approve, withdraw, new). A button that needs the row
  the user picked (a row action) stays an `item ... action` on the screen's table or list, because the toolbar has no row.
- Preview and Play open the command's form dialog for an entry (or run it after the confirmation); Studio's feature editor has a
  Toolbar section that adds a button and links it to a command of the feature. All of it is authoring syntax like every screen
  construct: the printer writes it back where it was written.
- Verify: `screenplay <model> --warnaserror` is clean (no warning about a command the feature does not declare), then Preview or Play a screen of the feature and see the
  buttons in the template's toolbar; a destructive one asks first.

## Template references, categories and blueprints

- Every `layout`, `screen template` and `dialog template` declares a `category`; a template picker groups by it, and a
  missing one is `PLAY0720` (an error in a blueprint package build).
- A template is **own** (`template <Name>`) or from a **blueprint** (`template blueprint <Blueprint> <Name> [version "<range>"]`),
  assigned at a module, feature, slice or screen. The nearest assignment wins.
- `version` is `*`, an exact version, or a `^`, `~`, `>=`, `>`, `<=` or `<` range such as `"^1.4"`; anything else
  (`"latest"`) is `PLAY0721`.
- The blueprint must be listed under a `ui profile`'s `packages` (`PLAY0722`). Prefer a blueprint's template over copying
  its layout into the model: the reference stays in step with the blueprint's releases, a copy does not.
- `scopes slice statechange|stateview` narrows a template to one slice kind; a template outside its scope is `PLAY0631`.

## Sample data packs

- A `sample data <Name>` pack holds rows per shape (`shape <ReadModel>` then `row` with `<field> = <literal>`), and an
  optional `title` and `locale`. Fields must be properties of the read model (`PLAY0710`) and literals must fit
  their types (`PLAY0711`).
- **The active pack is chosen once, in the `ui profile`** (`sample data` then `active <Pack>` or `active blueprint <id>`),
  never per screen, slice or component. A pack the document does not declare is `PLAY0714`. `shape <Shape> maps to
  <ReadModel>` maps a differently named shape; `bind <ReadModel> to <Pack>` is a rare per-read-model override.
- Sample data is **design and preview only**: the executable model defers it and a production build, a backend or
  Chronicle never sees it. Never put sample values in a component or a screen, and never describe sample data as test data
  for specifications.
- Without a selected pack the preview uses the blueprint's default pack and then deterministic generated data.

## Shapes the compiler refuses

Each block below is compiled by the verification and must report exactly the code beside it.

A module that overrides a region may not link to another module:

```screenplay expect PLAY0750
layout AppShell
  category application
  content

module Sales
  feature Orders

module Support
  navigation sidebar overrides
    item sales title "Sales" target Sales
  feature Tickets
```

An extending module may not reuse an application item id:

```screenplay expect PLAY0747
layout AppShell
  category application
  content

  navigation sidebar
    item sales title "Sales" target Sales

module Sales
  navigation sidebar extends
    item sales title "Sales again" target Sales.Orders

  feature Orders
```

A button may not invoke a State View slice:

```screenplay expect PLAY0762
module Shop
  feature Orders
    slice StateView OrderList
      readmodel OrderRow
        title String

      screen OrderList
        toolbar actions
          button refresh title "Refresh" slice OrderList
```

A blueprint reference needs a readable range and a blueprint a profile lists:

```screenplay expect PLAY0721,PLAY0722
module Sales
  feature Orders
    slice StateView OrderList
      readmodel OrderRow
        title String

      screen OrderList
        template blueprint Hr Workspace version "latest"
          body
            title "Orders"

ui profile Desktop
  target platform web
  packages
    Crm
```

## Editing the shell through MCP

The shell is typed syntax. `read-ast` returns `NavigationRegionSyntax`, `NavigationItemSyntax`, `ToolbarButtonSyntax`,
`TemplateAssignmentSyntax`, `ScreenTemplateReferenceSyntax` and `SampleDataPackSyntax`; `propose-ast` edits them; a
proposal against a revision that moved is refused with `StaleRevision`; `apply` writes only after the diagnostics above
pass. Propose, show the difference, apply, reopen. Details: `cratis-screenplay-model-authoring`,
`references/screen-authoring-workflow.md`.

- **Switch a region's mode** with a `replace` of the region. Inherit to extend adds the module's own items; inherit to
  override copies the application's items, retargeted to the module's features; override or extend to inherit clears the
  items.
- **Add the first child** of a region, toolbar or item with `add` to `navigation`, `buttons` or `children` (Screenplay
  4.188.0 or later; an older MCP server refuses it as an unknown member).
- A refusal (`PLAY0750`, `PLAY0762`, `PLAY0722`, `PLAY0714`) is the compiler's own answer: change the proposal, never the
  rule.

## Render and verify

- `screenplay <model> --warnaserror` must report no error and no warning first; absent highlighting proves nothing.
- `cratis render <model> --name <App> --destination <out>` and `cratis run <model>` carry the shell: the topbar and sidebar
  render, a module's sidebar replaces or extends the application's, a button opens the slice's command form as a dialog,
  and the address reflects the active module and feature. The sample data pack is not served.
- Report authoring acceptance, executable admission and runtime rendering separately (see `SKILL.md`).

## Review checklist

- [ ] Each module's region mode is deliberate: inherit by default, extend to add, override only to replace.
- [ ] No module item targets another module; no id repeats between an extending module and the application.
- [ ] The logo is declared once, in the layout.
- [ ] Hierarchy is in the sidebar; sub-items target features of the parent's module.
- [ ] Every button names a State Change slice; none is left without a slice.
- [ ] A screen with a `template` keeps its toolbar inside a slot body.
- [ ] Buttons shared by every screen of a feature sit in a feature `toolbar`, each linked to a command of that feature, with `confirm` on the destructive ones.
- [ ] Every template has a `category`; every blueprint reference has a valid range and is listed in a profile's `packages`.
- [ ] The active sample data pack is selected once, in the `ui profile`, and no sample value sits in a component.
