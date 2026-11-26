<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/free-screens.md -->
# Free screens: declare a screen first, let a slice adopt it

## Contents

- When a screen is free
- Declare a free screen and its needs
- Shapes: inferred and explicit
- Adopt the screen and provide each need
- Two needs of one shape: aliases
- Names, navigation, storylines and sample data
- Severities and the release gate
- Design-only screens
- Diagnostics
- Traps
- Checklist

Requires Screenplay 6.32.0 or later, whose contract lists the free-screen diagnostics (check `screenplay --version` and the toolchain skill's `screenplay-contract.json`). An older compiler rejects a top-level `shape` or `screen` with `PLAY0001`. Syntax the installed compiler rejects is never papered over with a stub: report the missing release.

## When a screen is free

A `screen` is one construct that may be declared at the **top level of a document, in a module, in a feature or in a slice**. A screen declared inside a slice is simply a free screen declared in that slice's scope and adopted by it, so an existing model reads, prints and binds exactly as before.

Declare the screen outside a slice when it exists before the slice that serves it (a designer's screen, a storyline step), when two slices share it, or when it is a navigation target nothing implements yet. Keep it in the slice when exactly one slice serves it. A screen is declared once per scope (`PLAY1152`); two scopes may each declare a screen of the same name, and a reference then resolves from where it is written, then by unique match.

Which slice should serve a screen, and what its read model must supply: [screens before slices](../../cratis-screenplay-slice-design/references/screens-before-slices.md). Editing these constructs through MCP proposals: [free screens through typed proposals](../../cratis-screenplay-model-authoring/references/free-screens-proposals.md).

## Declare a free screen and its needs

`data <Shape>[]` (a list) or `data <Shape>` (one value) with **no `via query`** is a **need**: data the screen reads and an adopting slice provides. A screen that names where its data comes from (`data <ReadModel> via query <Query>`) is an ordinary screen.

```screenplay expect PLAY1164
shape Invoice
  number String
  customer String
  amount Decimal
  dueDate Date

screen InvoiceList
  title "Invoices"
  data Invoice[]
  table Invoice
    column number label "#"
    column amount
    on row-click navigate to InvoiceDetails by number

screen InvoiceDetails
  title "Invoice"
  data Invoice
  summary Invoice
    field number label "Number"
    field customer label "Customer"
    field amount label "Amount"
```

A title is the `title` directive, never a quoted name after `screen` (`screen InvoiceList "Invoices"` is `PLAY0102`). The screen's other directives, its template, forms and interactions are written exactly as for a slice screen ([screen-composition-contract.md](screen-composition-contract.md)).

## Shapes: inferred and explicit

The shape of a need is a contract with two sources:

- **Inferred.** The fields the screen reads of the need (table columns, summary fields, the key of a row click, bound paths) are the contract the compiler derives. Nothing else is required, so a screen can be drawn first.
- **Explicit.** A top-level `shape <Name>` tightens it with typed fields, written like a read model's properties: `<name> <Type> [optional]`, plus an optional `description`. A shape has **no key, identifier, subject or generated modifier** (it is a contract on data, not stored state), is declared once, and only at the top level of a document (`PLAY1149`, `PLAY1151`).

Declare the shape explicitly when the types matter: several adopters, sample data to check, or a reviewer who needs the contract written down. A concept (`InvoiceNumber`) stands for its underlying primitive when a provision's types are compared.

## Adopt the screen and provide each need

A slice adopts a screen with `uses screen <Name>` and provides each need from one of its queries with `data <Need> via query <Query> [by <param>]`. Where the read model's property name differs from the shape's field, a nested `<field> from <path>` maps it; a name that matches needs no line.

```screenplay
shape Invoice
  number String
  customer String
  amount Decimal

screen InvoiceList
  title "Invoices"
  data Invoice[]
  table Invoice
    column number label "#"
    column amount

screen InvoiceDetails
  title "Invoice"
  data Invoice
  summary Invoice
    field number label "Number"
    field amount label "Amount"

module Invoicing
  feature Paying
    slice StateView ListInvoices
      readmodel InvoiceRow
        invoiceNumber String
        customer String
        amount Decimal
      query ListInvoices => InvoiceRow[]
      query GetInvoice => InvoiceRow optional
        by invoiceNumber String
      uses screen InvoiceList
        data Invoice via query ListInvoices
          number from invoiceNumber
      uses screen InvoiceDetails
        data Invoice via query GetInvoice by invoiceNumber
          number from invoiceNumber
```

The provision names the need without `[]`; the need's cardinality decides which query fits. The adoption is **many to many**: a slice adopts several screens and a screen is adopted by several slices. Every provision goes under the one `uses screen` line (`PLAY1154`). Hooks (`button ... slice X`) and "the slice's screen" for a State Change input work on an adopted screen exactly as on one declared inline.

What the compiler checks in a provision:

- the query exists and returns a list for a list need, a single value for a single one (`PLAY1159`);
- every field the screen reads, and every field its explicit `shape` declares, is a property of the query's read model, by name or through `from` (`PLAY1160`), with the type the shape declares (`PLAY1161`);
- `by` names a parameter the query declares (`PLAY1162`);
- a `from` mapping names a field the screen reads or the shape declares (`PLAY1163`);
- the provision targets a need the screen has (`PLAY1156`), never data the screen already sources with its own `data <ReadModel> via query` (`PLAY1157`), and provides it once (`PLAY1158`).

Fix a failure in the slice (the read model, the query, the mapping), not by loosening the shape until it passes. A field with no origin is a missing read model property, never a value invented in the screen.

## Two needs of one shape: aliases

A screen that needs the same shape twice tells the needs apart with `as <alias>`. The alias is the need's name in the screen's directives and in the provision. Two needs with one name are `PLAY1153`.

```screenplay
shape Invoice
  number String
  amount Decimal

screen Overview
  title "Overview"
  data Invoice[] as overdue
  data Invoice[] as open
  table overdue
    column number
  table open
    column number

module Invoicing
  feature Paying
    slice StateView ListOverdue
      readmodel Row
        number String
        amount Decimal
      query ListOverdue => Row[]
      query ListOpen => Row[]
      uses screen Overview
        data overdue via query ListOverdue
        data open via query ListOpen
```

## Names, navigation, storylines and sample data

- **Names resolve from the declaration site.** A query, command or screen a free screen names resolves inside-out from where the screen is declared, then by unique match anywhere in the model. An adopting slice never changes what a name means. Ambiguous or unresolved names stay warnings, as for a slice screen; commands and screens a free screen names have no contract beyond that.
- **Navigation reaches every screen.** `navigate to`, `show` and a storyline `screen` step resolve to a free screen or a slice screen alike, adopted or not.
- **Sample data may name a shape.** `sample data ... shape <Name>` checks its rows against the declared `shape`'s typed fields (`PLAY0710`, `PLAY0711`), as it does against a read model; rows for a screen nobody has adopted can exist before any read model does.

```screenplay expect PLAY1164
shape Invoice
  number String
  amount Decimal

screen InvoiceList
  title "Invoices"
  data Invoice[]
  table Invoice
    column number

screen InvoiceDetails
  title "Invoice"
  data Invoice
  summary Invoice
    field number label "Number"

storyline PayAnInvoice "A clerk pays an invoice"
  screen InvoiceList "Pick the invoice"
  screen InvoiceDetails "Check what is owed"

sample data Demo
  shape Invoice
    row
      number = "INV-1001"
      amount = 12.5
```

## Severities and the release gate

| Situation | Result |
| --- | --- |
| An adopted screen has a need no provision supplies | Warning `PLAY1155` while the model is being built |
| Warnings are errors (`screenplay <model> --warnaserror`, `cratis screenplay validate --warnings-as-errors`, a gate file's `warningsAsErrors`) | The same need fails the gate; exit 1 |
| Executable screen admission (the executable export that carries screens) | Refused with a reason naming the screen and the shape |
| A screen no slice adopts | Information `PLAY1164`; never fails any gate |
| A provision that does not hold, a malformed form, a duplicate shape, screen, need or adoption | Error (see Diagnostics) |

There is no separate release flag: the release gate is the warnings-as-errors gate every Screenplay model already runs. A work-in-progress model keeps compiling with `PLAY1155`; a release does not.

## Design-only screens

A free screen no slice adopts is **design-only**. It never blocks the rest of the model, never enters the executable model, and is not part of the `screenplay-ui/1` contract, so the renderer does not draw it. A screen an adopting slice carries reaches the UI contract once per adopting slice, with the data that slice's provisions resolve mapped back to the screen's fields. Report design-only screens as design intent, not as delivered screens; adopt them before claiming a flow renders.

## Diagnostics

| Code | Severity | Meaning |
| --- | --- | --- |
| `PLAY1149` | Error | A `shape` is malformed or not at the top level of a document, or a field carries a key, identifier, subject or generated modifier. |
| `PLAY1150` | Error | A `uses screen`, provision or mapping is malformed. |
| `PLAY1151` | Error | A `shape` is declared more than once. |
| `PLAY1152` | Error | A screen is declared more than once in one scope. |
| `PLAY1153` | Error | Two needs of a screen share a name; add `as <alias>`. |
| `PLAY1154` | Error | A slice adopts the same screen twice. |
| `PLAY1155` | Warning | An adopted screen has a need the slice does not provide. |
| `PLAY1156` | Error | A provision names a need the screen does not have. |
| `PLAY1157` | Error | A provision overrides data the screen already sources. |
| `PLAY1158` | Error | A need is provided more than once. |
| `PLAY1159` | Error | A list query provides a single need, or the reverse. |
| `PLAY1160` | Error | A field the screen reads is not a property of the providing read model. |
| `PLAY1161` | Error | A read model property has another type than the explicit shape declares. |
| `PLAY1162` | Error | A provision's `by` is not a parameter of the query. |
| `PLAY1163` | Error | A mapping names a field the screen does not read and the shape does not declare. |
| `PLAY1164` | Information | A screen no slice adopts is design-only. |

## Traps

- **A quoted name after `screen`.** Write `title "..."`; `screen InvoiceList "Invoices"` is `PLAY0102`.
- **Shape inside a module.** A `shape` is a top-level declaration; nested in a module, feature or imported file it is `PLAY1149`.
- **Provision for data the screen sources itself.** `data <ReadModel> via query` on the screen already binds it; a provision of it is `PLAY1157`. Drop the provision or make the screen's line a need.
- **One query for a list need and a single need.** Provide a list need from a list query and a single need from a single-value query.
- **Adopting without providing.** `uses screen X` with no provision compiles with `PLAY1155` and fails a warnings-as-errors gate; it is never a finished adoption.
- **Counting a design-only screen as delivered.** It is not in the UI contract or the executable model.

## Checklist

- [ ] Every free screen is declared once per scope, with a `title`; a shape is declared once at the top level.
- [ ] Every need has a shape; a screen that needs a shape twice uses aliases.
- [ ] Every adopted screen provides every need from a query of the right cardinality; names that differ are mapped with `from`.
- [ ] `screenplay <model> --warnaserror` reports no `PLAY1155`; remaining `PLAY1164` screens are deliberately unadopted.
- [ ] Storylines, `navigate to` and `show` name screens that exist, free or slice-declared.
