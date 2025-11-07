# FromEvery Attribute

The `FromEvery` attribute allows you to set properties that should be updated for every event that the projection processes, regardless of the specific event type. This is particularly useful for tracking metadata like timestamps, sequence numbers, or other audit fields that should be updated whenever any event affects the read model.

## Overview

Unlike event-specific attributes like `SetFrom<TEvent>` which only apply when a specific event type occurs, `FromEvery` applies to all events that are part of the projection. This ensures that certain properties are always kept up-to-date across all event types.

## Basic Usage

### Mapping from Event Context Properties

The most common use case is to map from event context properties like the occurrence timestamp:

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

    [FromEvery(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset LastUpdated);
```

In this example, `LastUpdated` will be set to the event's occurrence time for every event that affects this inventory item, whether it's a product registration, items being added, or items being removed.

### Available Event Context Properties

You can map to any property available on the `EventContext`:

```csharp
public record AuditableEntity(
    [Key] Guid Id,

    [FromEvery(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset LastModified,

    [FromEvery(contextProperty: nameof(EventContext.SequenceNumber))]
    ulong LastEventSequence,

    [FromEvery(contextProperty: nameof(EventContext.CorrelationId))]
    string LastCorrelationId);
```

## Mapping from Event Properties

You can also map from properties that exist on every event type in your projection. When you specify a property name, the attribute will attempt to read that property from each event:

```csharp
public record OrderStatus(
    [Key] Guid Id,

    [FromEvery(property: "Status")]
    string CurrentStatus);
```

This assumes that all events in the projection have a `Status` property. If an event doesn't have the specified property, the mapping is skipped for that event.

## Convention-Based Mapping

If you don't specify either `property` or `contextProperty`, the attribute will use the read model property name to look for a matching property on each event:

```csharp
public record Product(
    [Key] Guid Id,

    [FromEvery]  // Looks for 'Version' property on all events
    int Version);
```

## Combining with Other Attributes

`FromEvery` works seamlessly with other projection attributes. Each event can trigger both event-specific mappings and the `FromEvery` mapping:

```csharp
public record UserProfile(
    [Key] Guid Id,

    [SetFrom<UserCreated>(nameof(UserCreated.Name))]
    [SetFrom<UserNameChanged>(nameof(UserNameChanged.NewName))]
    string Name,

    [SetFrom<UserCreated>(nameof(UserCreated.Email))]
    [SetFrom<UserEmailChanged>(nameof(UserEmailChanged.NewEmail))]
    string Email,

    [FromEvery(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset LastUpdated);
```

When a `UserNameChanged` event occurs:

1. The `Name` property is updated via `SetFrom<UserNameChanged>`
2. The `LastUpdated` property is updated via `FromEvery`

## Use Cases

### Audit Timestamps

Track when a read model was last modified:

```csharp
[FromEvery(contextProperty: nameof(EventContext.Occurred))]
DateTimeOffset LastModified
```

### Event Sequencing

Keep track of the last event sequence number:

```csharp
[FromEvery(contextProperty: nameof(EventContext.SequenceNumber))]
ulong LastEventSequence
```

### Correlation Tracking

Maintain the correlation ID of the last operation:

```csharp
[FromEvery(contextProperty: nameof(EventContext.CorrelationId))]
string LastOperationId
```

### Causation Tracking

Track who or what caused the last change:

```csharp
[FromEvery(contextProperty: nameof(EventContext.CausedBy))]
string LastModifiedBy
```

## Comparison with Fluent API

The `FromEvery` attribute provides the same functionality as the `FromEvery()` method in fluent projections:

**Fluent Projection:**

```csharp
public class InventoryStatusProjection : IProjectionFor<InventoryStatus>
{
    public void Define(IProjectionBuilderFor<InventoryStatus> builder) => builder
        .AutoMap()
        .FromEvery(_ => _
            .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred))
        .From<ProductRegisteredInInventory>()
        .From<ItemsAddedToInventory>()
        .From<ItemsRemovedFromInventory>();
}
```

**Model-Bound Projection:**

```csharp
[FromEvent<ProductRegisteredInInventory>]
public record InventoryStatus(
    [Key] Guid Id,
    string ProductName,
    int CurrentStock,
    [FromEvery(contextProperty: nameof(EventContext.Occurred))] DateTimeOffset LastUpdated);
```

Both approaches produce identical results, but the model-bound approach keeps the metadata definition directly on the read model.

## Best Practices

1. **Use for metadata**: `FromEvery` is ideal for tracking metadata like timestamps, sequence numbers, and correlation IDs
2. **Avoid business logic**: Don't use `FromEvery` for business-critical properties that should only change under specific conditions
3. **Context properties preferred**: When possible, prefer mapping from event context properties over event properties for consistency
4. **Clear naming**: Use property names that clearly indicate they're updated by all events (e.g., `LastUpdated`, `LastModified`)

## Limitations

- The `FromEvery` attribute applies to all events in the projection - you cannot selectively exclude specific event types
- When mapping from event properties, if an event doesn't have the specified property, the mapping is silently skipped
- Only one `FromEvery` attribute can be applied per property (multiple updates to the same property from different event context fields should use the fluent API instead)
