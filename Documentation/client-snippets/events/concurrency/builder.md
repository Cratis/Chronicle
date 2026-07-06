```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

[EventType]
public record ConcurrencyMoneyDeposited(decimal Amount);

[EventType]
public record ConcurrencyMoneyWithdrawn(decimal Amount);

public class ConcurrencyAccountTransactionService(IEventLog eventLog)
{
    public async Task ProcessTransaction(EventSourceId accountId, decimal amount)
    {
        var concurrencyScope = new ConcurrencyScopeBuilder()
            .WithEventSourceId(accountId)
            .WithSequenceNumber(15)
            .WithEventStreamType("Transactions")
            .WithEventType<ConcurrencyMoneyDeposited>()
            .WithEventType<ConcurrencyMoneyWithdrawn>()
            .Build();

        await eventLog.Append(
            accountId,
            new ConcurrencyMoneyDeposited(amount),
            concurrencyScope: concurrencyScope);
    }
}
```
