```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reducers;

public record DepositTotal(decimal Amount);

[FromEventSource<AccountEventSource>("Transactions")]
public class DepositTotalReducer : IReducerFor<DepositTotal>
{
    public DepositTotal Deposited(MoneyDeposited @event, DepositTotal? current, EventContext context) =>
        new((current?.Amount ?? 0m) + @event.Amount);
}
```
