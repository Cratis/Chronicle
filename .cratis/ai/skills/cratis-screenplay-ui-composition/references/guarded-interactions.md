<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/guarded-interactions.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Choose a gesture's whole action list

`on click`, `on double click` and `on select` accept ordered block-form `when`
alternatives and an optional final `otherwise`. Use the same strict condition
language and data-item subject as [guarded screen actions](../../cratis-screenplay-read-surface/references/guarded-actions.md):
`item.<path>` compared with a literal, joined by `and`, `or` and parentheses.
At least one `when` and a nonempty action list in every branch are required.
Do not mix alternatives with plain actions or `where`; a one-line
`when … execute …` belongs to labeled actions, not interactions.

This complete model validates on the standalone compiler:

```screenplay
module Billing
  feature Invoices
    slice StateView InvoiceHistory
      event InvoiceRecorded
        status String
      readmodel InvoiceView
        invoiceId String
        status String
      projection Invoices => InvoiceView
        from InvoiceRecorded
          invoiceId = $eventSourceId
          status = status
      query InvoiceById => InvoiceView optional
        by invoiceId String
      screen InvoiceDetails
        data InvoiceView via query InvoiceById by invoiceId
        table InvoiceView
          column status
          on double click
            when item.status == "draft"
              notify info "The draft can be edited"
              refresh InvoiceById
            otherwise
              notify info "Only drafts can be edited"
```

The first matching branch runs once on the rendered item at gesture time, without
refetch or re-evaluation while its list runs. No match uses fallback or does
nothing; **no subject does nothing, including fallback**. Failure, denial or an
unavailable command never falls through; backend commands retain their rules.
Unlike labeled actions, no choice was displayed beforehand to refresh at click.

A table click/double click uses the activated row; select uses the newly selected
item, and clearing selection runs nothing. Elsewhere use the nearest enclosing
single item or selected row, with guarded actions' sibling precedence. Check named
behaviors at each `uses` site. Component `context` or `selectedItem` bindings do
not supply the subject in this version; unresolved/equally near subjects report
PLAY0346.

Submit, event, change, interval and application triggers have no alternatives.
The one-line `on row-click navigate to … by …` remains unchanged. Opaque `where`
on click/double click/select is deprecated (PLAY0564): strict item conditions warn
and C# offers a typed repair to one `when` with no fallback; other guard text is
Information without repair. Other triggers' `where` remains unchanged. Repairs
require individual review, canonical formatting, preserve comments and refuse
comment loss; TypeScript diagnoses but offers no repair. PLAY0563 has a C# repair
expanding a one-line alternative to the block form. Neither repair supports
pinned evidence.

UI is deferred from backend ESM (PLAY0269), not executable gesture proof. The
board draws a static prototype; renderers must explicitly admit alternatives or
report unsupported, never flatten them into plain actions.

Authority: Screenplay `interactions.md`, `diagnostics.md`,
`mcp/authoring-tools.md`.
