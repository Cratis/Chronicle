```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record ConfiguredBalanceAdded(decimal Amount);

[Reducer("verified-balances", eventSequence: "account-events")]
[FilterEventsByTag("verified")]
[EventSourceType("Account")]
[EventStreamType("Payments")]
[Tag("finance")]
public record ConfiguredSelfBalance(decimal Balance)
{
    public static ConfiguredSelfBalance Reduce(ConfiguredBalanceAdded @event, ConfiguredSelfBalance? current) =>
        new((current?.Balance ?? 0m) + @event.Amount);
}
```
