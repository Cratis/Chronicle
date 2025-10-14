# Projection with custom properties

When auto-mapping isn't sufficient, you can explicitly map properties from events to your read model. This gives you full control over how data is transformed and mapped.

## Explicit property mapping

Instead of using `AutoMap()`, use `Set()` methods to explicitly define property mappings:

```csharp
using Cratis.Chronicle.Projections;

public class AccountProjection : IProjectionFor<Account>
{
    public void Define(IProjectionBuilderFor<Account> builder) => builder
        .From<AccountOpened>(_ => _
            .Set(m => m.AccountNumber).To(e => e.Number)
            .Set(m => m.CustomerName).To(e => e.Owner.Name)
            .Set(m => m.Balance).To(42.0m)
            .Set(m => m.IsActive).To(true)
            .Set(m => m.OpenedAt).To(e => e.Timestamp))
        .From<MoneyDeposited>(_ => _
            .Set(m => m.Balance).To(e => e.Amount)
            .Set(m => m.LastTransaction).To(e => e.Timestamp));
}
```

## Combining AutoMap with explicit mapping

You can use `AutoMap()` at the top level to automatically map matching properties, then add explicit mappings for specific transformations:

```csharp
public class AccountProjection : IProjectionFor<Account>
{
    public void Define(IProjectionBuilderFor<Account> builder) => builder
        .AutoMap()  // Automatically maps matching properties
        .From<AccountOpened>(_ => _
            .Set(m => m.CustomerName).To(e => e.Owner.Name)  // Custom mapping for nested property
            .Set(m => m.IsActive).To(true))                  // Custom mapping for constant
        .From<MoneyDeposited>();  // Uses AutoMap for all properties
}
```

`AutoMap()` works recursively, automatically mapping:

- Properties with matching names and compatible types
- Nested objects and their properties
- Collections and arrays

You can also use `AutoMap()` explicitly for each event type instead of at the projection level:

```csharp
.From<AccountOpened>(_ => _.AutoMap())
.From<MoneyDeposited>(_ => _.AutoMap())

## Read model definition

The read model can have different property names and types than the events:

```csharp
public record Account(
    string AccountNumber,
    string CustomerName,
    decimal Balance,
    bool IsActive,
    DateTimeOffset OpenedAt,
    DateTimeOffset? LastTransaction);
```

## Event definitions

Events can have different structures than the read model:

```csharp
[EventType]
public record AccountOpened(
    string Number,
    Customer Owner,
    DateTimeOffset Timestamp);

[EventType]
public record MoneyDeposited(
    decimal Amount,
    DateTimeOffset Timestamp);

public record Customer(string Name, string Email);
```

## Property mapping options

You can map properties in several ways:

- **From event property**: `.Set(m => m.CustomerName).To(e => e.Owner.Name)`
- **From constant value**: `.Set(m => m.IsActive).To(true)`
- **From computed value**: `.Set(m => m.DisplayName).To(e => $"{e.FirstName} {e.LastName}")`
- **From event source ID**: `.Set(m => m.Id).ToEventSourceId()`

## Multiple events

A single projection can handle multiple event types, each with its own property mappings. Properties are updated incrementally as events are processed.

In the example above:

- `AccountOpened` sets initial values for all properties
- `MoneyDeposited` only updates `Balance` and `LastTransaction`
- Other properties retain their previous values

This approach gives you precise control over how your read models are built from events.
