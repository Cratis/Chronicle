<!-- cratis-ai-managed: skills/cratis-screenplay-read-surface/references/screen-levels.md -->
# Screen levels and constructs

Read this when writing a `screen` beyond the intent level, or choosing which level to use.

The examples below are excerpts: each screen sits in a slice, and the queries,
commands and screens it names are declared elsewhere.

**Level 2 — structure.** Named sections, tables and summaries filling a template's
slots.

```screenplay excerpt
screen InvoiceDetails
  template MasterDetail
    sidebar
      data InvoiceDetailsReadModel via query GetInvoice by invoiceId
      summary InvoiceDetailsReadModel
        field invoiceNumber label "Invoice #"
      section actions
        action CancelInvoice
    main
      section lineItems
        table lineItems
          column lineNumber label "#"
          column quantity   label "Qty"
          on row-click navigate to InvoiceLineDetail by lineNumber
```

**Level 3 — inline code.** The surrounding Screenplay context supplies the typed
data contract; the inline block receives it as `Props`. Languages: `react`,
`typescript`, `html`, `csharp`.

Constructs: `title`, `data … via query … [by <param>]`, `action <Command>` with
`label` and `navigate to <Screen> [by <param>]`, `section <name>`,
`table <target>` with `column <property> [label]` and
`on row-click navigate to <Screen> [by <param>]`, `summary <ReadModel>` with
`field <property> label`, `template <Name>` with slot bodies, `show <Screen> by <param> from <table>.selection`
in a slot body, and `fits dialog, drawer, pane` (where a State Change screen opens; `cratis-screenplay-ui-composition`).

The screen's `data` and `action` declarations are the typed context for later UI
composition. Component `context`, toolbar parameters, dialog inputs, slot routes
and manual form columns must all trace back to this screen contract, a form's
command, a selected item, or `$context`. If a generated app shows a default list
or empty panel because it could not consume one of those directives, treat that as
a dropped UI construct, not as an acceptable Level 1 screen.

`on row-click navigate to …` is the one-line table navigation form. What a click,
selection, submit or screen entry *does* beyond that — confirm, execute, refresh,
open a dialog, branch on success or failure — is an interaction: an inline `on`
block or a named `behavior` attached with `uses`. See
`cratis-screenplay-ui-composition`. Screens and interactions are deferred by default
(information `PLAY0269`) and admitted on request as ESM v42 with `screenplay export --screens`; they never block binding.
