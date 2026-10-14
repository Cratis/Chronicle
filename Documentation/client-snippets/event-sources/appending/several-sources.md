```csharp
using Cratis.Chronicle.EventSequences;

public class Transfers(IEventLog eventLog)
{
    // Two event sources, one atomic append: both events are written, or neither is.
    public Task<AppendManyResult> Transfer(EventSourceId from, EventSourceId to, decimal amount, string month) =>
        eventLog.AppendMany(
        [
            new EventForEventSourceId(from, new MoneyWithdrawn(amount))
            {
                EventSource = typeof(AccountEventSource),
                EventStream = "Transactions",
                EventStreamId = month
            },
            new EventForEventSourceId(to, new MoneyDeposited(amount))
            {
                EventSource = typeof(AccountEventSource),
                EventStream = "Transactions",
                EventStreamId = month
            }
        ]);
}
```
