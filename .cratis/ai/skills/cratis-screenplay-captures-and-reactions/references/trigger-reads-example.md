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

- `reads` sits **under a trigger**, beside the values it takes and its effects.
  Each trigger declares its own.
- **`by` names a value that trigger takes** (`invoiceId` above). Naming anything
  else is warning `PLAY0442`.
- **Clock triggers read whole views.** `every` and `at` take no values, so
  `reads <View>` works under them but `by` is error `PLAY0443`.
- **Aliases follow the command rules.** A view read more than once needs an
  alias on every read (`PLAY0410`), aliases are unique per trigger (`PLAY0411`),
  and an alias must not match a trigger value (`PLAY0412`). A view no projection
  in the document produces is warning `PLAY0177`.
- **`reads` is a directive in a trigger body.** A trigger value named `reads` is
  written `@reads`. A bare `reads` line is error `PLAY0175`, and
  `reads <PrimitiveType>` is warning `PLAY0444`; both messages name the escape.
- `for each <View>` is reserved for a future view-driven trigger. It is not a
  trigger in the verified Screenplay versions (`PLAY0137`).
