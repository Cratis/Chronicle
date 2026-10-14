```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

public class CatchUpPosting(IEventLog eventLog)
{
    public async Task<string> PostTwoMonths(EventSourceId accountId, decimal january, decimal february)
    {
        EventForEventSourceId[] events =
        [
            new(accountId, new MoneyDeposited(january))
            {
                EventSource = typeof(AccountEventSource),
                EventStream = "Transactions",
                EventStreamId = "2026-01"
            },
            new(accountId, new MoneyDeposited(february))
            {
                EventSource = typeof(AccountEventSource),
                EventStream = "Transactions",
                EventStreamId = "2026-02"
            }
        ];

        try
        {
            var result = await eventLog.AppendMany(events);
            return result.IsSuccess ? "Posted" : "Rejected";
        }
        catch (IncompatibleConcurrencyScopesForEventSource exception)
        {
            // Nothing was appended. The two events need guards on different streams of one event source id.
            return exception.Message;
        }
    }
}
```
