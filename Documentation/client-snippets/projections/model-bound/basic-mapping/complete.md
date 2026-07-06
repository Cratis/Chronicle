```csharp title="Complete balance projection"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record BankAccountOpened(string AccountName, decimal InitialBalance);

[EventType]
public record BankAccountRenamed(string NewName);

[EventType]
public record FundsDeposited(decimal Amount);

[EventType]
public record FundsWithdrawn(decimal Amount);

public record BankAccount(
    [Key] Guid Id,

    [SetFrom<BankAccountOpened>(nameof(BankAccountOpened.AccountName))]
    [SetFrom<BankAccountRenamed>(nameof(BankAccountRenamed.NewName))]
    string Name,

    [SetFrom<BankAccountOpened>(nameof(BankAccountOpened.InitialBalance))]
    [AddFrom<FundsDeposited>(nameof(FundsDeposited.Amount))]
    [SubtractFrom<FundsWithdrawn>(nameof(FundsWithdrawn.Amount))]
    decimal Balance);
```
