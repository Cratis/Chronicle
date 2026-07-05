```csharp title="Add from an event"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record AccountOpenedForDeposits(decimal InitialBalance);

[EventType]
public record DepositMadeForBalance(decimal Amount);

public record DepositAccount(
    [Key] Guid Id,

    [SetFrom<AccountOpenedForDeposits>(nameof(AccountOpenedForDeposits.InitialBalance))]
    [AddFrom<DepositMadeForBalance>(nameof(DepositMadeForBalance.Amount))]
    decimal Balance);
```
