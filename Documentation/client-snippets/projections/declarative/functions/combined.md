```csharp
[EventType]
public record DecFunctionsTransaction(decimal Amount);

public record DecFunctionsTransactionSummary(
    int TransactionCount,
    decimal TotalAmount,
    int ProcessedEvents);

public class DecFunctionsTransactionSummaryProjection : IProjectionFor<DecFunctionsTransactionSummary>
{
    public void Define(IProjectionBuilderFor<DecFunctionsTransactionSummary> builder) => builder
        .From<DecFunctionsTransaction>(_ => _
            .Count(m => m.TransactionCount)
            .Add(m => m.TotalAmount).With(e => e.Amount)
            .Increment(m => m.ProcessedEvents));
}
```
