```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record ReducersIndexDepositMade(decimal Amount);

[EventType]
public record ReducersIndexWithdrawalMade(decimal Amount);

public record ReducersIndexAccountBalance(decimal Balance, DateTimeOffset LastUpdated);

public class ReducersIndexAccountBalanceReducer : IReducerFor<ReducersIndexAccountBalance>
{
    public ReducersIndexAccountBalance Deposited(ReducersIndexDepositMade @event, ReducersIndexAccountBalance? current, EventContext context)
    {
        var currentBalance = current?.Balance ?? 0m;
        return new ReducersIndexAccountBalance(currentBalance + @event.Amount, context.Occurred);
    }

    public ReducersIndexAccountBalance WithdrawalMade(ReducersIndexWithdrawalMade @event, ReducersIndexAccountBalance? current, EventContext context)
    {
        var currentBalance = current?.Balance ?? 0m;
        return new ReducersIndexAccountBalance(currentBalance - @event.Amount, context.Occurred);
    }
}
```
