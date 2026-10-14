```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

public class CatchUpPostingWithOneScope(IEventLog eventLog)
{
    public async Task<bool> PostTwoMonths(EventSourceId accountId, decimal january, decimal february)
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

        // One explicit scope for the account that suits both events: every month of the Transactions stream.
        var tail = await eventLog.GetTailSequenceNumber(eventSourceId: accountId, eventStreamType: "Transactions");
        if (tail.IsUnavailable)
        {
            // Nothing in the stream yet: expect that no event appears before the append.
            tail = EventSequenceNumber.BeforeFirst;
        }

        var scope = new ConcurrencyScopeBuilder()
            .WithEventSourceId(accountId)
            .WithEventStreamType("Transactions")
            .WithSequenceNumber(tail)
            .Build();

        var result = await eventLog.AppendMany(
            events,
            concurrencyScopes: new Dictionary<EventSourceId, ConcurrencyScope> { [accountId] = scope });

        return result.IsSuccess;
    }
}
```
