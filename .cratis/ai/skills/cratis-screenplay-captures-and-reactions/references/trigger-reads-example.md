<!-- cratis-ai-managed: skills/cratis-screenplay-captures-and-reactions/references/trigger-reads-example.md -->
# Trigger reads example

## Trigger `reads` excerpt

```screenplay excerpt
slice Automation ChaseOverdueInvoices
  reaction RemindOnDueDate
    description "Reminds the customer when an invoice falls due unpaid"
    when InvoiceFellDue
      invoiceId
      reads InvoiceBalance as balance by invoiceId
      invokes SendPaymentReminder
        invoiceId = invoiceId
        channel   = "email"
      file Reactions/RemindOnDueDate.cs
  reaction SweepOverdueInvoices
    description "Re-checks every overdue invoice each morning"
    at 08:00
      reads OverdueInvoice
      file Reactions/SweepOverdueInvoices.cs
```
