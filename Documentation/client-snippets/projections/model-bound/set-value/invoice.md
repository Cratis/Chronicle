```csharp
[EventType]
public record MbSetValueInvoiceIssued(decimal Amount);

[EventType]
public record MbSetValueInvoicePaid;

public record MbSetValueInvoice(
    [Key]
    Guid Id,

    [SetFrom<MbSetValueInvoiceIssued>(nameof(MbSetValueInvoiceIssued.Amount))]
    decimal Amount,

    [SetValue<MbSetValueInvoiceIssued>("issued")]
    [SetValue<MbSetValueInvoicePaid>("paid")]
    string Status);
```
