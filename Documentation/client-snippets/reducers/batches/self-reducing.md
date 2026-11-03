```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

[EventType]
public record BatchMoneyDeposited(decimal Amount);

[EventType]
public record BatchMoneyWithdrawn(decimal Amount);

public record AccountBalance(decimal Balance)
{
    [Observes<BatchMoneyDeposited>]
    [Observes<BatchMoneyWithdrawn>]
    public static AccountBalance? Reduce(EventBatch events, AccountBalance? current, BatchContext context) =>
        events.Aggregate(current ?? new AccountBalance(0), (account, e) => e.Event switch
        {
            BatchMoneyDeposited deposited => account with { Balance = account.Balance + deposited.Amount },
            BatchMoneyWithdrawn withdrawn => account with { Balance = account.Balance - withdrawn.Amount },
            _ => account
        });
}
```
