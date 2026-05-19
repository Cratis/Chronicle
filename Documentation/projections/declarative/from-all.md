# Projection with FromAll

The `FromAll()` method configures a projection to receive and process every event in the event log, regardless of event type. This complements the type-specific `From<TEvent>()` calls and is useful for tracking metadata properties that should be updated whenever any event occurs.

## Basic Usage

```csharp
public class UserProfileProjection : IProjectionFor<UserProfile>
{
    public void Define(IProjectionBuilderFor<UserProfile> builder) => builder
        .AutoMap()
        .FromAll(_ => _
            .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred))
        .From<UserCreated>()
        .From<UserEmailChanged>()
        .From<UserNameChanged>();
}
```

`LastUpdated` is updated for every event type — not just the three listed in `From<>`.

## Available Mappings

### Event Context Properties

Map to any property on the event context:

```csharp
.FromAll(_ => _
    .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred)
    .Set(m => m.LastEventSequence).ToEventContextProperty(c => c.SequenceNumber)
    .Set(m => m.LastCorrelationId).ToEventContextProperty(c => c.CorrelationId))
```

### Event Source ID

```csharp
.FromAll(_ => _
    .Set(m => m.EventSourceId).ToEventSourceId())
```

## Examples

### User Profile with Audit

```csharp
public record UserProfile(
    string UserId,
    string Name,
    string Email,
    DateTimeOffset LastUpdated,
    ulong LastEventSequence);

public class UserProfileProjection : IProjectionFor<UserProfile>
{
    public void Define(IProjectionBuilderFor<UserProfile> builder) => builder
        .AutoMap()
        .FromAll(_ => _
            .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred)
            .Set(m => m.LastEventSequence).ToEventContextProperty(c => c.SequenceNumber))
        .From<UserCreated>()
        .From<UserEmailChanged>()
        .From<UserNameChanged>();
}
```

### Order with Full Tracking

```csharp
public record Order(
    string OrderId,
    string OrderNumber,
    string CustomerId,
    string Status,
    DateTimeOffset LastModified,
    IEnumerable<OrderItem> Items);

public class OrderProjection : IProjectionFor<Order>
{
    public void Define(IProjectionBuilderFor<Order> builder) => builder
        .AutoMap()
        .FromAll(_ => _
            .Set(m => m.LastModified).ToEventContextProperty(c => c.Occurred))
        .From<OrderCreated>(_ => _
            .Set(m => m.Status).To(_ => "Placed"))
        .From<OrderShipped>(_ => _
            .Set(m => m.Status).To(_ => "Shipped"))
        .Children(m => m.Items, children => children
            .IdentifiedBy(e => e.ProductId)
            .AutoMap()
            .From<ItemAddedToOrder>(_ => _
                .UsingKey(e => e.ProductId)));
}
```

## Difference Between `FromAll()` and `FromEvery()`

Both `FromAll()` and `FromEvery()` apply mappings to all events. Use `FromAll()` to express that the projection subscribes to all event types; use `FromEvery()` when you only need common per-event side effects and have explicit `From<TEvent>()` calls elsewhere.

| Feature | `FromAll()` | `FromEvery()` |
|---------|-------------|----------------|
| Applies to | All event types | All event types |
| Typical use | Subscribe to all events | Audit / common metadata |
| Include children | Yes (default) | Configurable |

## Common Use Cases

### Audit Timestamps

```csharp
.FromAll(_ => _
    .Set(m => m.LastModified).ToEventContextProperty(c => c.Occurred))
```

### Event Sequencing

```csharp
.FromAll(_ => _
    .Set(m => m.LastEventSequence).ToEventContextProperty(c => c.SequenceNumber))
```

### Correlation Tracking

```csharp
.FromAll(_ => _
    .Set(m => m.LastOperationId).ToEventContextProperty(c => c.CorrelationId))
```

## Best Practices

1. **Use for metadata**: `FromAll()` is best suited for audit-style properties (timestamps, sequence numbers, correlation IDs).
2. **Avoid business logic**: Do not use `FromAll()` for properties that should only change under specific event conditions.
3. **Prefer context properties**: Mapping from event context properties is more robust than mapping from event data properties, because all events share the same context shape.
