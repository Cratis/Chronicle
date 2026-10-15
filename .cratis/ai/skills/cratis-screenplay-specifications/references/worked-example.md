<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/worked-example.md -->
## Contents

- Worked example

## Worked example

A command with an authorization gate, a validation rule and a uniqueness
constraint, and a read model fed by its event. The `for` values select ESM v2.

```screenplay
concept InvoiceId : Uuid
concept InvoiceNumber : String
policy IsAccountant
  require role "Accountant"
module Invoicing
  feature Registration
    slice StateChange RegisterInvoice
      command RegisterInvoice
        invoiceId     InvoiceId identifier
        invoiceNumber InvoiceNumber
        authorize IsAccountant
        validate
          invoiceNumber not empty message "Invoice number is required"
        produces event InvoiceRegistered
          invoiceNumber InvoiceNumber = invoiceNumber
      constraint UniqueInvoiceNumber
        unique invoiceNumber on InvoiceRegistered
      specification RegisteringAnInvoice
        given caller
          authenticated
          role "Accountant"
        when RegisterInvoice
          invoiceId     = "9c858901-8a57-4791-81fe-4c455b099bc9"
          invoiceNumber = "INV-000123"
        then InvoiceRegistered
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          invoiceNumber = "INV-000123"
        then readmodel InvoiceSummary
          invoiceId     = "9c858901-8a57-4791-81fe-4c455b099bc9"
          invoiceNumber = "INV-000123"
      specification RejectingAnEmptyInvoiceNumber
        given caller
          authenticated
          role "Accountant"
        when RegisterInvoice
          invoiceId     = "9c858901-8a57-4791-81fe-4c455b099bc9"
          invoiceNumber = ""
        then error "Invoice number is required"
      specification RejectingANumberAnotherInvoiceHolds
        given caller
          authenticated
          role "Accountant"
        given InvoiceRegistered
          for "0f5f5f7f-0f6f-4f47-9f39-5c1f2f0a1a9f"
          invoiceNumber = "INV-000123"
        when RegisterInvoice
          invoiceId     = "9c858901-8a57-4791-81fe-4c455b099bc9"
          invoiceNumber = "INV-000123"
        then error
      specification DenyingACallerWithoutTheRole
        given caller
          authenticated
        when RegisterInvoice
          invoiceId     = "9c858901-8a57-4791-81fe-4c455b099bc9"
          invoiceNumber = "INV-000123"
        then denied
      specification ProjectingAnAppendedInvoice
        when append InvoiceRegistered
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          invoiceNumber = "INV-000123"
        then query InvoiceById
          arguments
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          result
            invoiceNumber = "INV-000123"
    slice StateView InvoiceLookup
      readmodel InvoiceSummary
        invoiceId     InvoiceId
        invoiceNumber InvoiceNumber
      query InvoiceById => InvoiceSummary optional
        by invoiceId InvoiceId
      projection InvoiceSummaries => InvoiceSummary
        from InvoiceRegistered
          invoiceId     = $eventSourceId
          invoiceNumber = invoiceNumber
      specification LookingUpAnExistingInvoice
        given readmodel InvoiceSummary
          invoiceId     = "9c858901-8a57-4791-81fe-4c455b099bc9"
          invoiceNumber = "INV-000001"
        when query InvoiceById
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        then result
          invoiceNumber = "INV-000001"
```

- Inline events are ordinary named facts in `given` and `then`. Assert their
  destination with `for`, not a payload identifier; inline declaration does not
  change fixture or assertion syntax.
- `RejectingANumberAnotherInvoiceHolds` needs `for`: without it the `given`
  event lands on the command's own event source, and re-claiming your own value
  is not a violation.
- `ProjectingAnAppendedInvoice` exercises the projection without running the
  command. A `when append` specification may sit in a `StateView` slice, even for an
  event another slice declares, as long as the event's producer gives it one
  unambiguous destination type (binding resolves it from commands in other slices);
  otherwise `for` fails with `PLAY0273`. Only `when <Command>` needs the command's
  own slice.
- `LookingUpAnExistingInvoice` performs the query as its action. It binds to
  exactly what `then query` with `arguments` and `result` would, so on a version
  before v4.48.0 write it that way instead.
