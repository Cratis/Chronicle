```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

public readonly record struct KeyedAccountId(string Value)
{
    public static implicit operator EventSourceId(KeyedAccountId id) => new(id.Value);
}

[EventType]
public record KeyedMoneyWithdrawn(decimal Amount);

[EventType]
public record KeyedMoneyDeposited(decimal Amount);

public class KeyedTransferService(IEventLog eventLog)
{
    public Task<AppendManyResult> Transfer(string transferId, KeyedAccountId fromAccount, KeyedAccountId toAccount, decimal amount)
    {
        var events = new[]
        {
            new EventForEventSourceId(fromAccount, new KeyedMoneyWithdrawn(amount))
            {
                IdempotencyKey = new IdempotencyKey($"transfer:{transferId}:withdrawn")
            },
            new EventForEventSourceId(toAccount, new KeyedMoneyDeposited(amount))
            {
                IdempotencyKey = new IdempotencyKey($"transfer:{transferId}:deposited")
            }
        };

        // Any keyed event makes the batch keyed; events without a key are appended normally.
        return eventLog.AppendMany(events);
    }
}
```
