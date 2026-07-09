```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

// Warning CHR0036: Reducer 'Chr0036BalanceReducer' declares mutable state '_runningTotal'.
// Reducers must be stateless for deterministic replay.
public class Chr0036BalanceReducer : IReducerFor<Chr0036Balance>
{
    decimal _runningTotal;

    public Chr0036Balance Reduce(Chr0036AmountDeposited @event, Chr0036Balance? current)
    {
        _runningTotal += @event.Amount;
        return new(_runningTotal);
    }
}

[EventType]
public record Chr0036AmountDeposited(decimal Amount);

public record Chr0036Balance(decimal Total);
```
