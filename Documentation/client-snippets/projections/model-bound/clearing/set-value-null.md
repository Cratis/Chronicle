```csharp title="The same clear, written as a null SetValue"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbClearingInvoiceIssued(string Reference);

[EventType]
public record MbClearingInvoiceVoided;

[FromEvent<MbClearingInvoiceIssued>]
public record MbClearingInvoice(
    [Key]
    Guid Id,

    [SetValue<MbClearingInvoiceVoided>(null)]
    string? Reference);
```
