```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

[EventType]
public record ConcurrencyMoneyWithdrawnForTransfer(decimal Amount);

[EventType]
public record ConcurrencyMoneyDepositedForTransfer(decimal Amount);

public class ConcurrencyTransferService(IEventLog eventLog)
{
    public async Task TransferMoney(EventSourceId fromAccount, EventSourceId toAccount, decimal amount)
    {
        var events = new[]
        {
            new EventForEventSourceId(fromAccount, new ConcurrencyMoneyWithdrawnForTransfer(amount)),
            new EventForEventSourceId(toAccount, new ConcurrencyMoneyDepositedForTransfer(amount))
        };

        var concurrencyScopes = new Dictionary<EventSourceId, ConcurrencyScope>
        {
            [fromAccount] = new ConcurrencyScopeBuilder()
                .WithEventSourceId(fromAccount)
                .WithSequenceNumber(50)
                .WithEventType<ConcurrencyMoneyWithdrawnForTransfer>()
                .Build(),

            [toAccount] = new ConcurrencyScopeBuilder()
                .WithEventSourceId(toAccount)
                .WithSequenceNumber(25)
                .WithEventType<ConcurrencyMoneyDepositedForTransfer>()
                .Build()
        };

        await eventLog.AppendMany(events, concurrencyScopes: concurrencyScopes);
    }
}
```
