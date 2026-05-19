# FromAll Attribute

The `[FromAll]` attribute marks a read model property so that it is updated for every event the projection processes, regardless of event type. It is the model-bound equivalent of calling `FromAll()` in a fluent projection.

## Overview

Unlike event-specific attributes such as `[SetFrom<TEvent>]`, which only apply when a particular event type occurs, `[FromAll]` applies to all events that are part of the projection. This makes it ideal for audit metadata such as last-updated timestamps or event sequence numbers.

## Basic Usage

### Mapping from Event Context Properties

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record InventoryStatus(
    [Key]
    Guid Id,

    [SetFrom<ProductRegisteredInInventory>(nameof(ProductRegisteredInInventory.ProductName))]
    string ProductName,

    [AddFrom<ItemsAddedToInventory>(nameof(ItemsAddedToInventory.Quantity))]
    [SubtractFrom<ItemsRemovedFromInventory>(nameof(ItemsRemovedFromInventory.Quantity))]
    int CurrentStock,

    [FromAll(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset LastUpdated);
```

`LastUpdated` is set to the event's occurrence time for every event that affects this read model — product registration, items added, or items removed.

### Available Event Context Properties

```csharp
public record AuditableEntity(
    [Key] Guid Id,

    [FromAll(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset LastModified,

    [FromAll(contextProperty: nameof(EventContext.SequenceNumber))]
    ulong LastEventSequence,

    [FromAll(contextProperty: nameof(EventContext.CorrelationId))]
    string LastCorrelationId);
```

## Mapping from Event Properties

When you specify a property name via the `property` parameter, the attribute reads that property from each event. If an event does not have the specified property, the mapping is silently skipped.

```csharp
public record OrderStatus(
    [Key] Guid Id,

    [FromAll(property: "Status")]
    string CurrentStatus);
```

This assumes all events in the projection have a `Status` property.

## Convention-Based Mapping

When neither `property` nor `contextProperty` is specified, the attribute matches the read model property name against each event's properties by convention:

```csharp
public record Product(
    [Key] Guid Id,

    [FromAll]  // Looks for 'Version' property on all events
    int Version);
```

## Combining with Other Attributes

`[FromAll]` works alongside event-specific attributes. When an event occurs, all applicable attributes are evaluated:

```csharp
public record UserProfile(
    [Key] Guid Id,

    [SetFrom<UserCreated>(nameof(UserCreated.Name))]
    [SetFrom<UserNameChanged>(nameof(UserNameChanged.NewName))]
    string Name,

    [SetFrom<UserCreated>(nameof(UserCreated.Email))]
    [SetFrom<UserEmailChanged>(nameof(UserEmailChanged.NewEmail))]
    string Email,

    [FromAll(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset LastUpdated);
```

When a `UserNameChanged` event occurs:

1. `Name` is updated by `[SetFrom<UserNameChanged>]`.
2. `LastUpdated` is updated by `[FromAll]`.

## Comparison with Fluent API

`[FromAll]` is the attribute equivalent of `FromAll()` in fluent projections:

**Fluent Projection:**

```csharp
public class InventoryStatusProjection : IProjectionFor<InventoryStatus>
{
    public void Define(IProjectionBuilderFor<InventoryStatus> builder) => builder
        .AutoMap()
        .FromAll(_ => _
            .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred))
        .From<ProductRegisteredInInventory>()
        .From<ItemsAddedToInventory>()
        .From<ItemsRemovedFromInventory>();
}
```

**Model-Bound Projection:**

```csharp
[FromEvent<ProductRegisteredInInventory>]
[FromEvent<ItemsAddedToInventory>]
[FromEvent<ItemsRemovedFromInventory>]
public record InventoryStatus(
    [Key] Guid Id,
    string ProductName,
    int CurrentStock,
    [FromAll(contextProperty: nameof(EventContext.Occurred))] DateTimeOffset LastUpdated);
```

Both produce identical results. The model-bound approach keeps the metadata definition on the read model type.

## Use Cases

### Audit Timestamps

```csharp
[FromAll(contextProperty: nameof(EventContext.Occurred))]
DateTimeOffset LastModified
```

### Event Sequencing

```csharp
[FromAll(contextProperty: nameof(EventContext.SequenceNumber))]
ulong LastEventSequence
```

### Correlation Tracking

```csharp
[FromAll(contextProperty: nameof(EventContext.CorrelationId))]
string LastOperationId
```

### Causation Tracking

```csharp
[FromAll(contextProperty: nameof(EventContext.CausedBy))]
string LastModifiedBy
```

## Best Practices

1. **Use for metadata**: `[FromAll]` is ideal for tracking timestamps, sequence numbers, and correlation IDs.
2. **Avoid business logic**: Do not use `[FromAll]` for properties that should only change under specific conditions.
3. **Prefer context properties**: Event context properties (`contextProperty`) are more robust than event data properties because all events share the same context shape.
4. **Clear naming**: Use property names that clearly indicate they are updated by all events — for example, `LastUpdated`, `LastModified`.

## Limitations

- Only one `[FromAll]` attribute can be applied per property.
- When mapping from event properties (`property` parameter), missing properties are silently skipped.
