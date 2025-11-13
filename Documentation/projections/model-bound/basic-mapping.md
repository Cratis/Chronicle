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

## SetFromContext

The `SetFromContext` attribute maps properties from the event context to the read model. This is useful for capturing event metadata like timestamps, sequence numbers, or correlation IDs for specific event types.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record Order(
    [Key]
    Guid Id,

    [SetFrom<OrderPlaced>(nameof(OrderPlaced.CustomerName))]
    string CustomerName,

    [SetFromContext<OrderPlaced>(nameof(EventContext.Occurred))]
    DateTimeOffset OrderedAt,

    [SetFromContext<OrderShipped>(nameof(EventContext.Occurred))]
    DateTimeOffset? ShippedAt);
```

In this example:

- `OrderedAt` is set to the occurrence time when an `OrderPlaced` event is processed
- `ShippedAt` is set to the occurrence time when an `OrderShipped` event is processed

### Available Event Context Properties

The `EventContext` provides several properties you can map to:

```csharp
public record AuditedEntity(
    [Key] Guid Id,

    [SetFromContext<EntityCreated>(nameof(EventContext.Occurred))]
    DateTimeOffset CreatedAt,

    [SetFromContext<EntityCreated>(nameof(EventContext.SequenceNumber))]
    ulong CreatedAtSequence,

    [SetFromContext<EntityCreated>(nameof(EventContext.CorrelationId))]
    CorrelationId CreatedByCorrelation,

    [SetFromContext<EntityUpdated>(nameof(EventContext.Occurred))]
    DateTimeOffset? LastUpdatedAt);
```

### Context Property Name Convention

If the event context property name matches the read model property name, you can omit the property name parameter:

```csharp
public record Event(
    [Key] Guid Id,

    [SetFromContext<EventHappened>] DateTimeOffset Occurred,  // Maps to EventContext.Occurred
    [SetFromContext<EventHappened>] ulong SequenceNumber);    // Maps to EventContext.SequenceNumber
```

### SetFromContext vs FromEvery

The key difference between `SetFromContext` and `FromEvery`:

- **SetFromContext** - Maps event context properties for **specific event types**
- **FromEvery** - Maps properties that should be updated for **every event** affecting the projection

Use `SetFromContext` when you want to track when specific events occurred (e.g., "when was this order placed?"), and use `FromEvery` when you want to track the most recent update across all events (e.g., "when was this entity last modified by any event?").

```csharp
public record OrderWithAudit(
    [Key] Guid Id,

    // Specific event context properties
    [SetFromContext<OrderPlaced>(nameof(EventContext.Occurred))]
    DateTimeOffset PlacedAt,

    [SetFromContext<OrderShipped>(nameof(EventContext.Occurred))]
    DateTimeOffset? ShippedAt,

    // Updated by any event
    [FromEvery(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset LastModified);
```

## Event Flow

When events are processed:

1. **AccountOpened** - Sets both `Name` and `Balance` (initial balance)
2. **DepositMade** - Adds to `Balance`
3. **WithdrawalMade** - Subtracts from `Balance`
4. **AccountRenamed** - Updates `Name`

The projection automatically maintains the current state of the account based on the events.
