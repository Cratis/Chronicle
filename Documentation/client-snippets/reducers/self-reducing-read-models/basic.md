```csharp
using Cratis.Chronicle.Events;

[EventType]
public record SelfBalanceAdded(decimal Amount);

[EventType]
public record SelfBalanceCleared;

public record SelfAccountBalance(decimal Balance)
{
    public static SelfAccountBalance Reduce(SelfBalanceAdded @event, SelfAccountBalance? current) =>
        new((current?.Balance ?? 0m) + @event.Amount);

    public static SelfAccountBalance? Reduce(SelfBalanceCleared @event, SelfAccountBalance? current) => null;
}
```
