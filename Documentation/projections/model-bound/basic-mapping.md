# Basic Property Mapping

Model-bound projections support three fundamental property mapping operations: SetFrom, AddFrom, and SubtractFrom.

## SetFrom

The `SetFrom` attribute maps a property from an event to the read model. This is the most common mapping operation.

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record User(
    [Key]
    Guid Id,

    [SetFrom<UserRegistered>(nameof(UserRegistered.Email))]
    string Email,

    [SetFrom<UserRegistered>(nameof(UserRegistered.Name))]
    string Name);
```

### Property Name Convention

If the property name on the event matches the read model property name, you can omit the property name parameter:

```csharp
public record User(
    [Key] Guid Id,
    [SetFrom<UserRegistered>] string Email,  // Maps to UserRegistered.Email
    [SetFrom<UserRegistered>] string Name);   // Maps to UserRegistered.Name
```

### Multiple Events

You can set a property from multiple different events:

```csharp
public record Account(
    [Key] Guid Id,

    [SetFrom<AccountOpened>(nameof(AccountOpened.AccountName))]
    [SetFrom<AccountRenamed>(nameof(AccountRenamed.NewName))]
    string Name);
```

## AddFrom

The `AddFrom` attribute adds values from event properties to the read model property. This is useful for accumulating values like balances or totals.

```csharp
public record Account(
    [Key] Guid Id,

    [SetFrom<AccountOpened>(nameof(AccountOpened.InitialBalance))]
    [AddFrom<DepositMade>(nameof(DepositMade.Amount))]
    decimal Balance);
```

When a `DepositMade` event occurs, the `Amount` is added to the current `Balance`.

## SubtractFrom

The `SubtractFrom` attribute subtracts values from event properties. This is commonly used with AddFrom to model increases and decreases.

```csharp
public record Account(
    [Key] Guid Id,

    [SetFrom<AccountOpened>(nameof(AccountOpened.InitialBalance))]
    [AddFrom<DepositMade>(nameof(DepositMade.Amount))]
    [SubtractFrom<WithdrawalMade>(nameof(WithdrawalMade.Amount))]
    decimal Balance);
```

## Complete Example

Here's a complete example showing all three mapping operations:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

// Events
[EventType]
public record AccountOpened(string AccountName, decimal InitialBalance);

[EventType]
public record DepositMade(decimal Amount);

[EventType]
public record WithdrawalMade(decimal Amount);

[EventType]
public record AccountRenamed(string NewName);

// Read Model
public record BankAccount(
    [Key]
    Guid Id,

    [SetFrom<AccountOpened>(nameof(AccountOpened.AccountName))]
    [SetFrom<AccountRenamed>(nameof(AccountRenamed.NewName))]
    string Name,

    [SetFrom<AccountOpened>(nameof(AccountOpened.InitialBalance))]
    [AddFrom<DepositMade>(nameof(DepositMade.Amount))]
    [SubtractFrom<WithdrawalMade>(nameof(WithdrawalMade.Amount))]
    decimal Balance);
```

## Event Flow

When events are processed:

1. **AccountOpened** - Sets both `Name` and `Balance` (initial balance)
2. **DepositMade** - Adds to `Balance`
3. **WithdrawalMade** - Subtracts from `Balance`
4. **AccountRenamed** - Updates `Name`

The projection automatically maintains the current state of the account based on the events.
