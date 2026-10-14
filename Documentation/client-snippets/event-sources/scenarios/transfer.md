```csharp
using Cratis.Chronicle.EventSequences;

public class AccountTransfers(IEventLog eventLog)
{
    // Uses the Account definition and events from the first scenario.
    public Task<AppendManyResult> Move(EventSourceId from, EventSourceId to, string month, decimal amount) =>
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
