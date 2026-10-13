<!-- cratis-ai-managed: skills/cratis-screenplay-event-modeling/references/invoicing-example.md -->
# Complete invoicing example (parent of the Step 5 and Step 6 excerpts)

One compiling document, in design mode (the `ListInvoices` list query blocks V3; the document
declares no specification, so nothing is lost for running), holding the `RegisterInvoice` slice
(Step 5) and the `InvoiceList` slice (Step 6) with the concepts, policy and events they rely on. The excerpts in
[nine-steps.md](nine-steps.md) are these slices, verbatim.

```screenplay
domain Acme.Invoicing

concept InvoiceId : Uuid
concept InvoiceNumber : String
concept InvoiceStatus : String

policy CanManageInvoice
  require role "Accountant"

module Invoicing
  feature InvoiceManagement
    description "Registering and managing the lifecycle of invoices"

    slice StateChange RegisterInvoice
      command RegisterInvoice
        invoiceId     InvoiceId identifier
        invoiceNumber InvoiceNumber
        authorize CanManageInvoice
        validate
          invoiceNumber not empty  message "Invoice number is required"
        produces event InvoiceRegistered
          invoiceNumber InvoiceNumber = invoiceNumber
          registeredAt DateTime = $context.occurred
      event InvoiceSent

    slice StateView InvoiceList
      readmodel InvoiceListReadModel
        invoiceId     InvoiceId
        invoiceNumber InvoiceNumber
        status        InvoiceStatus
      projection InvoiceList => InvoiceListReadModel
        from InvoiceRegistered
          invoiceId = $eventSourceId
          status = "draft"
        from InvoiceSent
          invoiceId = $eventSourceId
          status = "sent"
      query ListInvoices => InvoiceListReadModel[]
```
