```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record DepositMade(decimal Amount);

[EventType]
public record WithdrawalMade(decimal Amount);

public record AccountBalance(decimal Balance, DateTimeOffset LastUpdated);

public class AccountBalanceReducer : IReducerFor<AccountBalance>
{
    public AccountBalance Deposited(DepositMade @event, AccountBalance? current, EventContext context) =>
        new((current?.Balance ?? 0m) + @event.Amount, context.Occurred);

    public AccountBalance WithdrawalMade(WithdrawalMade @event, AccountBalance? current, EventContext context) =>
        new((current?.Balance ?? 0m) - @event.Amount, context.Occurred);
}
```
