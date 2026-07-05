```csharp
[EventType]
public record DecNotRewindablePaymentProcessed(string PaymentId, decimal Amount);

public record DecNotRewindableLedgerEntry(
    DateTimeOffset RecordedAt,
    string TransactionType);

public class DecNotRewindableTransactionLedgerProjection : IProjectionFor<DecNotRewindableLedgerEntry>
{
    public void Define(IProjectionBuilderFor<DecNotRewindableLedgerEntry> builder) => builder
        .NotRewindable()
        .AutoMap()
        .FromEvery(_ => _
            .Set(m => m.RecordedAt).ToEventContextProperty(c => c.Occurred))
        .From<DecNotRewindablePaymentProcessed>(_ => _
            .Set(m => m.TransactionType).ToValue("PAYMENT"));
}
```
