```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

public readonly record struct TaggedAccountId(string Value)
{
    public static implicit operator EventSourceId(TaggedAccountId id) => new(id.Value);
}

[EventType]
public record TaggedMoneyWithdrawn(decimal Amount);

[EventType]
public record TaggedMoneyDeposited(decimal Amount);

public class TaggedTransferService(IEventLog eventLog)
{
    public Task<AppendManyResult> Transfer(TaggedAccountId fromAccount, TaggedAccountId toAccount, decimal amount)
    {
        var events = new[]
        {
            new EventForEventSourceId(fromAccount, new TaggedMoneyWithdrawn(amount)),
            new EventForEventSourceId(toAccount, new TaggedMoneyDeposited(amount))
        };

        return eventLog.AppendMany(events, tags: ["transfer", "audit"]);
    }
}
```
