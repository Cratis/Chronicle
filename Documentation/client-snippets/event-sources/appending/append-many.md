```csharp
using Cratis.Chronicle.EventSequences;

public class MonthlyPosting(IEventLog eventLog)
{
    // Every event goes to the same event source id, stream and stream id.
    public Task<AppendManyResult> Post(EventSourceId accountId, string month, decimal deposit, decimal withdrawal) =>
        eventLog.AppendMany<AccountEventSource>(
            accountId,
            [new MoneyDeposited(deposit), new MoneyWithdrawn(withdrawal)],
            eventStream: "Transactions",
            eventStreamId: month);
}
```
