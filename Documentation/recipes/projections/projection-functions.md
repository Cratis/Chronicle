# Projection functions

Projections support several built-in functions for mathematical operations and counting. These functions allow you to perform calculations directly within projections without needing custom logic.

## Counting events

Use `Count()` to increment a counter each time an event is processed:

```csharp
public class UserActivityProjection : IProjectionFor<UserActivity>
{
    public void Define(IProjectionBuilderFor<UserActivity> builder) => builder
        .From<UserLoggedIn>(_ => _
            .Set(m => m.Username).To(e => e.Username)
            .Count(m => m.LoginCount))
        .From<UserPerformedAction>(_ => _
            .Count(m => m.ActionCount));
}
```

## Read model

The read model contains numeric properties for the counters:

```csharp
public record UserActivity(
    string Username,
    int LoginCount,
    int ActionCount);
```

## Increment and decrement

Use `Increment()` and `Decrement()` to add or subtract 1 from a property:

```csharp
public class InventoryProjection : IProjectionFor<Inventory>
{
    public void Define(IProjectionBuilderFor<Inventory> builder) => builder
        .From<ItemAdded>(_ => _
            .Set(m => m.ItemName).To(e => e.Name)
            .Increment(m => m.Quantity))
        .From<ItemRemoved>(_ => _
            .Decrement(m => m.Quantity));
}
```

These functions always change the value by exactly 1.

## Add and subtract with values

Use `Add()` and `Subtract()` to add or subtract specific values from event properties:

```csharp
public class AccountProjection : IProjectionFor<Account>
{
    public void Define(IProjectionBuilderFor<Account> builder) => builder
        .From<AccountOpened>(_ => _
            .Set(m => m.AccountNumber).To(e => e.Number)
            .Set(m => m.Balance).To(0m))
        .From<MoneyDeposited>(_ => _
            .Add(m => m.Balance).With(e => e.Amount))
        .From<MoneyWithdrawn>(_ => _
            .Subtract(m => m.Balance).With(e => e.Amount));
}
```

## Supported types

All projection functions work with these numeric types:

- `int`
- `long`
- `float`
- `double`
- `decimal`

The functions automatically handle type conversion and maintain the target property's type.

## Event definitions

```csharp
[EventType]
public record UserLoggedIn(string Username);

[EventType]
public record UserPerformedAction(string Username, string ActionType);

[EventType]
public record ItemAdded(string Name);

[EventType]
public record ItemRemoved(string Name);

[EventType]
public record AccountOpened(string Number);

[EventType]
public record MoneyDeposited(decimal Amount);

[EventType]
public record MoneyWithdrawn(decimal Amount);
```

## How functions work

1. **Initialization**: Properties start at 0 (or their default value) when first accessed
2. **Accumulation**: Functions apply their operations incrementally as events are processed
3. **Type safety**: Values are converted to match the target property type
4. **State preservation**: Current values are maintained between events

## Combining functions

You can use multiple functions in a single projection:

```csharp
.From<Transaction>(_ => _
    .Count(m => m.TransactionCount)
    .Add(m => m.TotalAmount).With(e => e.Amount)
    .Increment(m => m.ProcessedEvents))
```

These functions provide powerful aggregation capabilities while keeping projection logic simple and declarative.
