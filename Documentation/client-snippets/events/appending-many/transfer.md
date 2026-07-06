```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

public readonly record struct AccountId(string Value)
{
    public static implicit operator EventSourceId(AccountId id) => new(id.Value);
}

[EventType]
public record MoneyWithdrawn(decimal Amount);

[EventType]
public record MoneyDeposited(decimal Amount);

public class TransferService(IEventLog eventLog)
{
    public Task<AppendManyResult> Transfer(AccountId fromAccount, AccountId toAccount, decimal amount)
    {
        var events = new[]
        {
            new EventForEventSourceId(fromAccount, new MoneyWithdrawn(amount)),
            new EventForEventSourceId(toAccount, new MoneyDeposited(amount))
        };

        return eventLog.AppendMany(events);
    }
}
```
