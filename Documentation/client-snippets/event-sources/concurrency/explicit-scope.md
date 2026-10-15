```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

public class DecideThenDeposit(IEventLog eventLog)
{
    public async Task<bool> Withdraw(EventSourceId accountId, string month, Func<decimal> decide)
    {
        // Capture the position of this month's stream before you read the facts you decide on.
        var tail = await eventLog.GetTailSequenceNumber(
            eventSourceId: accountId,
            eventSourceType: "Account",
            eventStreamType: "Transactions",
            eventStreamId: month);

        var amount = decide();

        // An empty stream has no tail (Unavailable). Explicit scopes skip that value, so expect "no event yet".
        if (tail.IsUnavailable)
        {
            tail = EventSequenceNumber.BeforeFirst;
        }

        // ForEventSource names the source type, the stream type and the stream id from the definition.
        // The builder does not set the event source id or the position: you do.
        var scope = new ConcurrencyScopeBuilder()
            .WithEventSourceId(accountId)
            .ForEventSource<AccountEventSource>("Transactions", month)
            .WithSequenceNumber(tail)
            .Build();

        var result = await eventLog.Append<AccountEventSource>(
            accountId,
            new MoneyWithdrawn(amount),
            eventStream: "Transactions",
            eventStreamId: month,
            concurrencyScope: scope);

        return result.IsSuccess;
    }
}
```
