<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/application-shell.md -->
# The application shell: slots, outlets, hooks, template references and sample data

## Contents

- Default shell
- Slots and outlets
- One complete shell
- Navigation outlets: inherit, extend or override
- Outlets of actions, filters, tabs and stats
- Hooks: buttons that start a State Change slice
- Placement: where a slice's screen opens
- Template references, categories and blueprints
- Sample data packs
- Shapes the compiler refuses
- Editing the shell through MCP
- Render and verify
- Review checklist

Requires Screenplay 6.0 or later (`screenplay --version`) for slot roles, outlets, hooks, `fits` and `show`. A model
written for Screenplay 5 runs `screenplay migrate <folder> --from 5 --to 6` first (see `cratis-screenplay-modeling-lifecycle`);
an old form is an error whose message names that migration, so the code is the version, not the model.

Every construct here is authoring syntax. The executable model defers it (`PLAY0269`), Stage plans it from the syntax
tree, and none of it changes a backend. The Scene designer, the Stage runtime and Studio read the same lines, so the
Screenplay is the one place to write them.

## Default shell

Requires Stage 4.189.3 or later (the CLI that bundles it) for a rendered frontend to get a default application shell without a
`layout` or `ui profile`. Declare a `layout` and a `ui profile` only to customize that shell
(navigation, logo, templates, sample data), as below.

## Slots and outlets

Two words, two things. Never use one for the other.

- A **slot** is a region content is placed in. Each plain line in a layout or template body declares one, optionally
  followed, in this order, by a role (`page`, `pane` or `panel`), `routed` or `routed by <param>`, `primary` and
  `optional`: `content page routed primary`, `detail pane routed by sku optional`. A component's recursive fill point
  is `slot <name>`, and a navigation target or contribution names one with `slot <name>` / `destination slot <name>`.
- An **outlet** is a named list of items a layout, screen template or dialog template offers:
  `outlet <name> of actions|filters|tabs|stats|navigation [for item|for screen] [in <slot>]`, with its `default` items.
  `in <slot>` ties the list to the slot that shows it. A module, feature or slice screen changes it with
  `outlet <name> inherits|extends|overrides`, and any module or feature adds to it with `contribute to <name>`.

## One complete shell

```screenplay
layout AppShell
  category application
  topbar
  side panel
  content page routed primary

  outlet topbar of navigation in topbar
    logo image "/assets/acme.svg" title "Acme"
    default sales title "Sales" icon receipt target Sales
    default support title "Support" target Support slot content
  outlet sidebar of navigation in side
    default sales title "Sales" icon receipt target Sales
      item orders title "Orders" target Sales.Orders
      item returns title "Returns" target Sales.Orders.Returns slot side
    default support title "Support" target Support

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
  outlet sidebar extends
    item customers title "Customers" icon people target Sales.Customers
  outlet topbar inherits

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
      screen PlaceOrder
        fits dialog, drawer
        action PlaceOrder for screen

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
  outlet sidebar overrides
    item tickets title "Tickets" target Support.Tickets
  outlet topbar inherits

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

This follows the canonical shell vector (`screen-shell-navigation-positive` in `Cratis.Screenplay.CanonicalCorpus`).
Start from it; do not invent the syntax.

## Navigation outlets: inherit, extend or override

The `layout` declares each navigation outlet with `outlet <name> of navigation [in <slot>]` and lists the application's
items as `default` lines. A `module` (or a feature or slice screen, for the screens beneath it) states how it relates to an
outlet it changes; an outlet it does not mention **inherits** what is above it.

| Level says | Items shown | Choose it when |
| --- | --- | --- |
| nothing, or `outlet <name> inherits` | The inherited items | The level needs no navigation of its own (the default) |
| `outlet <name> extends` | The inherited items, then the level's own | It adds items (a sales sidebar that also lists Customers) and keeps the application's |
| `outlet <name> overrides` | The level's own items only | It replaces the whole list (a support area with its own sidebar) |

- Each outlet has its own mode: a module can extend the sidebar and inherit the topbar.
- The layout declares; it never says `inherits`, `extends` or `overrides`, and `inherits` lists no items (`PLAY0897`).
- `logo image "<path>" title "<text>"` belongs to the layout's navigation outlet only; a logo below the layout is `PLAY0744`.
- A navigation item is `<id> title "<text>" [icon <name>] [target <Module>[.<Feature>]] [slot <name>]`, after `default` on
  the layout and after `item` below it; any item nests sub-items as `item` lines. The title is a quoted string or
  `$strings.<key>`.
- An id is unique in the outlet, and an extending level never repeats an inherited id (`PLAY0905`). `hide <id>` drops an
  inherited item; `before <id>` or `after <id>` under an own item places it among the inherited ones (`PLAY0899` when the
  id is not inherited). Own items never move above inherited ones without `before`.
- `target` names a module (its first feature) or a feature path. An item with no target needs sub-items, and a target the
  document does not declare is `PLAY0748`. A module with no feature warns (`PLAY0751`).
- `slot` names a layout slot the target renders into; without it the target renders into the layout's primary slot. A slot
  no layout declares is `PLAY0749`.
- **A module's own items may only target that module** (`PLAY0750`). To link elsewhere, put the item in the layout. This
  matters when switching a module from inherit to override: retarget the copied items to the module's own features.
- An `overrides` outlet with no items leaves the list empty and warns (`PLAY0752`).

### `contribute to`: one entry from where it belongs

```screenplay excerpt
contribute to Navigation
  navigate to InvoiceList
  label "Invoices"
  order 20
  icon receipt
  destination slot details
```

- `contribute to <outlet>` sits directly on a `module` or a `feature` at any depth and works for any outlet kind. Use it
  for one entry a feature owns, next to its screens; use the outlet modes above for a tree or a whole list.
- `navigate`, `label` and `order` describe one navigation entry, all optional; `id`, `icon`, `presentation` and `group`
  are kept as written. Item lines (`button`, `group`, `filter`, `tab`, `stat`, `item`) add items of the outlet's kind
  after its own (`PLAY0900` otherwise).
- `destination slot <name>` opens the entry in a recursive slot a template or component declares with `slot <name>`
  (a plain region line does not count), `destination dialog <DialogTemplate>` in a dialog and
  `destination external "<route>"` outside the application; an undeclared slot or dialog is `PLAY0654`. A `navigate to`
  inside it takes the same `route`, `slot` and `parameter` lines as a screen's navigation.
- `navigate to <Screen> by <param>` is **not** string interpolation; it reuses the typed navigate binding a screen action
  uses.
- **Resolution:** the contribution's own module's templates first; with no local match, layouts and other modules
  together. Competing matches warn as ambiguous. An outlet nothing declares is `PLAY0903`; a document that uses a
  blueprint is exempt, because the blueprint's templates may declare it.

## Outlets of actions, filters, tabs and stats

A template offers more than navigation. Each kind takes its own item form, and an item of the wrong kind is `PLAY0900`:

| Kind | Item form |
| --- | --- |
| `actions` | `button <id> title "…" [slice <Slice>] [emphasis primary\|secondary\|quiet] [icon <name>] [for item\|for screen] [opens dialog\|drawer\|in slot <name>]`, or `group <id> title "…" [icon <name>]` holding `button` lines |
| `filters` | `filter <id> title "…" sets <Query>.<param>` - the parameter must be a `by` or `filter` parameter (`PLAY0901`) |
| `tabs` | `tab <id> title "…" shows <Screen>` - shown in the slot the outlet is `in` |
| `stats` | `stat <id> title "…" value from <Query>.<field>` - the query returns one result (`PLAY0902`) |
| `navigation` | `item <id> title "…" [icon] [target] [slot]`, as above |

```screenplay
module Catalog
  screen template ListPage
    category List
    header
    main
    outlet toolbar of actions in header
      default refresh title "Refresh" runs refresh
      default export title "Export"
    outlet filters of filters in header
    outlet stats of stats in header

  feature Products
    outlet toolbar extends
      hide export
      button add title "Add product" slice DraftProduct emphasis primary icon plus
        before refresh
    outlet filters extends
      filter status title "Status" sets AllProducts.status
    outlet stats extends
      stat drafts title "Drafts" value from ProductStats.drafts

    slice StateChange DraftProduct
      command DraftProduct
        sku String identifier
        produces ProductDrafted
          for sku
      event ProductDrafted
        sku String
      screen DraftProduct
        fits dialog
        action DraftProduct for screen

    slice StateView ProductCatalog
      readmodel ProductRow
        sku String
        status String
      readmodel ProductStats
        drafts Int
      query AllProducts => ProductRow[]
        filter status String
      query ProductStats => ProductStats
      screen ProductCatalog
        template ListPage
        outlet toolbar inherits
```

- Only a layout, screen template or dialog template declares an outlet (`PLAY0897` elsewhere); declaring or changing one
  twice is `PLAY0904`, and changing one nothing declares is `PLAY0903`.
- The levels apply in order: the template's defaults, the module, the feature and its parent features, then the screen.
- A `default` button with no `slice` is a placeholder a lower level fills, so it does not warn. Below the template, a
  button with no `slice` and no `runs` built-in warns (`PLAY0763`); never ship one.
- A dialog or drawer template declares its buttons as an actions outlet; `runs close`, `runs submit` and `runs refresh`
  are built-ins that need no slice.
- Choose an outlet on the template when the same actions, filters or tabs belong to many screens: the feature that owns
  the commands `extends` it once instead of repeating a toolbar on each screen. A screen that needs its own buttons keeps
  a screen `toolbar <name>` inside a slot body (Stage refuses a screen mixing a top-level `template` with other
  top-level directives).

## Hooks: buttons that start a State Change slice

A `button` that names a State Change `slice` is a **hook**. It opens the slice's screen, or runs its command directly.

- `slice` is a dotted path or any unambiguous suffix (`PLAY0764` when it matches several). It must name a slice the
  document declares (`PLAY0761`) and a State Change slice. A State View, Automation or Translate slice is `PLAY0762`.
- `for item` makes the hook wait for a selected row, which supplies the command's identifier; `for screen` acts on the
  screen's own record. Without it the outlet's `for` decides.
- A `runs` line under the button issues the command without a form. Every input must resolve without one (the
  identifier from the item or screen, generated values) or it is `PLAY0907`. A slice issued only by a `runs` hook needs
  no screen; a screenless slice whose hook cannot run, or names a place, is `PLAY0908`.
- `enabled when <condition>` reads `item.<field>` of the selected row; a `for screen` hook cannot read `item`
  (`PLAY0915`). `confirm "<text>"` (or `$strings.<key>`) asks first; use it for destructive or hard-to-undo commands.
- `authorize <Policy>` under a button limits who sees it.
- A hook counts as issuing its slice's command for `PLAY0533` when it `runs` or opens a slice that has a screen. Once
  the application declares navigation, a State Change screen nothing reaches issues nothing, so give each one a hook
  (or a navigation entry or action that leads to it).

```screenplay
module Catalog
  screen template ListPage
    category Lists
    main
    detail pane routed by sku optional
    outlet rowActions of actions for item

  feature Products
    outlet rowActions extends
      button price title "Set price" slice SetProductPrice opens in slot detail
      button withdraw title "Withdraw" slice WithdrawProduct
        runs
        enabled when item.status == "onSale"
        confirm "Withdraw this product?"

    slice StateChange SetProductPrice
      command SetProductPrice
        sku String identifier
        price Decimal
        produces PriceSet
          for sku
      event PriceSet
        sku String
      screen SetProductPrice
        fits pane, drawer
        action SetProductPrice for screen

    slice StateChange WithdrawProduct
      command WithdrawProduct
        sku String identifier
        produces ProductWithdrawn
          for sku
      event ProductWithdrawn
        sku String

    slice StateView ProductCatalog
      readmodel ProductRow
        sku String
        status String
      query AllProducts => ProductRow[]
      screen ProductCatalog
        template ListPage
          main
            table products
              column sku
              column status
          detail
            show ProductDetails by sku from products.selection

    slice StateView ProductDetails
      readmodel ProductDetail
        sku String
      query ProductBySku => ProductDetail
        by sku String
      screen ProductDetails
        data ProductDetail via query ProductBySku by sku
```

## Placement: where a slice's screen opens

The template offers places, the slice says which it fits, and the hook picks one.

- A template declares slots with roles; the page always offers a dialog and a drawer over it.
- **A slice's screen says what it fits**, in order of preference: `fits dialog, drawer, pane`. The places are `dialog`,
  `drawer`, `pane`, `panel` and `page`; without `fits` it fits anywhere. A malformed `fits` (an unknown or repeated place,
  a second line, or a template's `fits slot <name>` on a screen) is `PLAY0906`.
- **The hook picks automatically**: it opens the screen in its first fit the page has free. Write `opens dialog`,
  `opens drawer` or `opens in slot <name>` only when this hook needs another place than the automatic one.
- `opens in slot <name>` needs a slot the template declares (`PLAY0909`), that the hook's screen does not already fill
  (`PLAY0910`), and whose role the slice's screen lists in its `fits` (`PLAY0911`).
- `show <Screen> by <param> from <element>.selection` in a template slot shows another screen for the row a table or
  component selected. With a routed slot (`detail pane routed by sku`) the selection is in the address, so a shared link
  opens the same row. The line must stand in a template slot and key the screen by a parameter its data is keyed by
  (`PLAY0914`).

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
- **A collection of a declared top-level `type` uses nested rows** (decision 0136): write the
  collection field bare, then a `row` block per element with that element's fields beneath it, recursively. A bare
  field with no rows is an **explicit empty list**; omitting the field leaves it absent for generation or preview
  fallback, which is not the same. Primitive collections and a single composite object take no nested rows
  (`PLAY0711`), and scalar-list literals and JSON are not collection syntax. Preview needs matching Stage/Studio
  conversion support.
- **The active pack is chosen once, in the `ui profile`** (`sample data` then `active <Pack>` or `active blueprint <id>`),
  never per screen, slice or component. A pack the document does not declare is `PLAY0714`. `shape <Shape> maps to
  <ReadModel>` maps a differently named shape; `bind <ReadModel> to <Pack>` is a rare per-read-model override.
- Sample data is **design and preview only**: the executable model defers it and a production build, a backend or
  Chronicle never sees it. Never put sample values in a component or a screen, and never describe sample data as test data
  for specifications.
- Without a selected pack the preview uses the blueprint's default pack and then deterministic generated data.

## Shapes the compiler refuses

Each block below is compiled by the verification and must report exactly the code beside it.

A module that overrides an outlet may not link to another module:

```screenplay expect PLAY0750
layout AppShell
  category application
  content page routed primary
  outlet sidebar of navigation

module Sales
  feature Orders

module Support
  outlet sidebar overrides
    item sales title "Sales" target Sales
  feature Tickets
```

An extending level may not reuse an inherited item id:

```screenplay expect PLAY0905
layout AppShell
  category application
  content page routed primary

  outlet sidebar of navigation
    default sales title "Sales" target Sales

module Sales
  outlet sidebar extends
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

A hook that opens a slice's screen in a slot whose role the screen does not fit:

```screenplay expect PLAY0911
module Catalog
  screen template ListPage
    category Lists
    main
    detail pane
    outlet toolbar of actions in main

  feature Products
    outlet toolbar extends
      button price title "Set price" slice SetProductPrice opens in slot detail

    slice StateChange SetProductPrice
      command SetProductPrice
        sku String identifier
        price Decimal
        produces PriceSet
          for sku
      event PriceSet
        sku String
      screen SetProductPrice
        fits dialog
        action SetProductPrice for screen

    slice StateView ProductCatalog
      readmodel ProductRow
        sku String
      screen ProductCatalog
        template ListPage
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

The shell is typed syntax. `read-ast` returns `OutletSyntax` (on layouts, templates, modules, features and screens) with
its items, `NavigationItemSyntax`, `NavigationLogoSyntax`, `ToolbarButtonSyntax`, `TemplateAssignmentSyntax`,
`ScreenTemplateReferenceSyntax` and `SampleDataPackSyntax`; `propose-ast` edits them; a proposal against a revision that
moved is refused with `StaleRevision`; `apply` writes only after the diagnostics above pass. Propose, show the difference,
apply, reopen. Resolve member names with `syntax-schema`. Details: `cratis-screenplay-model-authoring`,
`references/screen-authoring-workflow.md`.

- **Switch an outlet's mode** with a `replace` of the level's outlet. Inherit to extend adds the level's own items;
  inherit to override copies the inherited items, retargeted to the module's features; override or extend to inherit
  clears the items.
- A refusal (`PLAY0750`, `PLAY0762`, `PLAY0905`, `PLAY0911`, `PLAY0722`, `PLAY0714`) is the compiler's own answer: change
  the proposal, never the rule.

## Render and verify

- `screenplay <model> --warnaserror` must report no error and no warning first; absent highlighting proves nothing.
- `cratis render <model> --name <App> --destination <out>` and `cratis run <model>` carry the shell: the navigation
  outlets render, a module's sidebar replaces or extends the application's, a hook opens the slice's screen where it
  fits, and the address reflects the active module, feature and routed slots. The sample data pack is not served.
- Report authoring acceptance, executable admission and runtime rendering separately (see `SKILL.md`).

## Review checklist

- [ ] Slots and outlets are not confused: regions are slots with roles, item lists are outlets of a kind.
- [ ] Each level's outlet mode is deliberate: inherit by default, extend to add, override only to replace.
- [ ] No module item targets another module; no id repeats between an extending level and what it inherits.
- [ ] The logo is declared once, in the layout's navigation outlet.
- [ ] Every hook names a State Change slice; a `runs` hook's inputs all resolve without a form.
- [ ] Each State Change screen says what it `fits`; a hook names a place only when it needs one other than the automatic choice.
- [ ] Buttons shared by every screen of a feature extend the template's actions outlet once, with `confirm` on the destructive ones.
- [ ] Every template has a `category`; every blueprint reference has a valid range and is listed in a profile's `packages`.
- [ ] The active sample data pack is selected once, in the `ui profile`, and no sample value sits in a component.
