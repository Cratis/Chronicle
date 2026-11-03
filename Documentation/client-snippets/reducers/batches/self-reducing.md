```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

[EventType]
public record MoneyDeposited(decimal Amount);

[EventType]
public record MoneyWithdrawn(decimal Amount);

public record Account(decimal Balance)
{
    [Observes<MoneyDeposited>]
    [Observes<MoneyWithdrawn>]
    public static Account? Reduce(EventBatch events, Account? current, BatchContext context) =>
        events.Aggregate(current ?? new Account(0), (account, e) => e.Event switch
        {
            MoneyDeposited deposited => account with { Balance = account.Balance + deposited.Amount },
            MoneyWithdrawn withdrawn => account with { Balance = account.Balance - withdrawn.Amount },
            _ => account
        });
}
```
