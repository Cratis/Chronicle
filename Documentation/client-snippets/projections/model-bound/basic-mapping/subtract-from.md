```csharp title="Subtract from an event"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record BalanceAccountOpened(decimal InitialBalance);

[EventType]
public record BalanceDepositMade(decimal Amount);

[EventType]
public record BalanceWithdrawalMade(decimal Amount);

public record BalanceAccount(
    [Key] Guid Id,

    [SetFrom<BalanceAccountOpened>(nameof(BalanceAccountOpened.InitialBalance))]
    [AddFrom<BalanceDepositMade>(nameof(BalanceDepositMade.Amount))]
    [SubtractFrom<BalanceWithdrawalMade>(nameof(BalanceWithdrawalMade.Amount))]
    decimal Balance);
```
