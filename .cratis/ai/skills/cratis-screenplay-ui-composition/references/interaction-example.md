<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/interaction-example.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Named and inline interaction example

Interaction triggers are `click`, `double click`, `select`, `submit`, `change`,
`load`/`unload`, `enter`/`leave`, `event <Event>`, `interval <n> <unit>` or a declared
application trigger. Actions are `execute`, `navigate to`/`navigate back`,
`open dialog`/`close dialog`, `refresh`, `set … to`, `notify`, `confirm` and `raise`.
A behavior bundles trigger-to-action bindings, inline (`on …`) or named
(`behavior` + `uses`); these closed vocabularies are not scripting.

The named behavior confirms before executing; the screen also refreshes on enter.
The inline event targets `invoiceId`, which the projection reads from event context.
Validate the complete model with `screenplay <root> --warnaserror`; that validates
source, not Scene runtime wiring.

```screenplay
concept InvoiceId : Uuid

behavior ConfirmThenExecute
  parameter command
  parameter message
  on click
    confirm message
      on success
        execute command

module Invoicing
  feature InvoiceManagement
    slice StateChange CancelInvoice
      command CancelInvoice
        invoiceId InvoiceId identifier
        produces event InvoiceCancelled
      screen CancelInvoiceScreen
        data InvoiceStatusView via query InvoiceStatusById by invoiceId
        uses ConfirmThenExecute
          command CancelInvoice
          message "Cancel this invoice?"
        on enter
          refresh InvoiceStatusById
            on failure
              notify error "The invoice could not be loaded"
    slice StateView InvoiceStatus
      readmodel InvoiceStatusView
        invoiceId InvoiceId
        cancelled Bool
      projection InvoiceStatuses => InvoiceStatusView
        from InvoiceCancelled
          invoiceId = $eventSourceId
          cancelled = true
      query InvoiceStatusById => InvoiceStatusView optional
        by invoiceId InvoiceId
```
