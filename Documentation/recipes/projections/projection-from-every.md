# Projection with FromEvery

The `FromEvery()` method allows you to set properties that will be updated for every event that the projection processes, regardless of the specific event type. This is particularly useful for tracking metadata like "last updated" timestamps or common audit fields.

## Basic FromEvery usage

Use `FromEvery()` to set properties that should be updated by all events in the projection:

```csharp
public class UserProfileProjection : IProjectionFor<UserProfile>
{
    public void Define(IProjectionBuilderFor<UserProfile> builder) => builder
        .FromEvery(_ => _
            .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred))
        .From<UserCreated>(_ => _
            .Set(m => m.Name).To(e => e.Name)
            .Set(m => m.Email).To(e => e.Email))
        .From<UserEmailChanged>(_ => _
            .Set(m => m.Email).To(e => e.NewEmail))
        .From<UserNameChanged>(_ => _
            .Set(m => m.Name).To(e => e.NewName));
}
```

In this example, `LastUpdated` will be set to the event's occurrence time for every event that affects this projection.

## Available FromEvery mappings

### Event context properties

Map to any event context property for all events:

```csharp
.FromEvery(_ => _
    .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred)
    .Set(m => m.LastEventSequence).ToEventContextProperty(c => c.SequenceNumber)
    .Set(m => m.LastCorrelationId).ToEventContextProperty(c => c.CorrelationId))
```

### Event source ID

Set the event source ID for all events:

```csharp
.FromEvery(_ => _
    .Set(m => m.EventSourceId).ToEventSourceId())
```

## Excluding child projections

By default, `FromEvery()` applies to events in child projections as well. You can exclude child projections:

```csharp
.FromEvery(_ => _
    .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred)
    .ExcludeChildProjections())
```

## FromEvery with children

When using `FromEvery()` with child projections, the parent properties are updated for all events, including those that only affect children:

```csharp
public class OrderProjection : IProjectionFor<Order>
{
    public void Define(IProjectionBuilderFor<Order> builder) => builder
        .FromEvery(_ => _
            .Set(m => m.LastModified).ToEventContextProperty(c => c.Occurred))
        .From<OrderCreated>(_ => _
            .Set(m => m.OrderNumber).To(e => e.OrderNumber)
            .Set(m => m.CustomerId).To(e => e.CustomerId))
        .Children(m => m.Items, children => children
            .IdentifiedBy(e => e.ProductId)
            .From<ItemAddedToOrder>(_ => _
                .UsingKey(e => e.ProductId)
                .Set(m => m.ProductName).To(e => e.ProductName)
                .Set(m => m.Quantity).To(e => e.Quantity))
            .From<ItemQuantityChanged>(_ => _
                .UsingKey(e => e.ProductId)
                .Set(m => m.Quantity).To(e => e.NewQuantity)));
}
```

In this example, `LastModified` on the order will be updated when:

- The order is created (`OrderCreated`)
- Items are added (`ItemAddedToOrder`)
- Item quantities change (`ItemQuantityChanged`)

## Multiple FromEvery calls

You can call `FromEvery()` multiple times, and the properties will be merged:

```csharp
.FromEvery(_ => _
    .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred))
.FromEvery(_ => _
    .Set(m => m.ModifiedBy).ToEventContextProperty(c => c.CausedBy))
```

## Read model example

```csharp
public record UserProfile(
    string UserId,
    string Name,
    string Email,
    DateTimeOffset LastUpdated,
    ulong LastEventSequence,
    string LastCorrelationId);

public record Order(
    string OrderId,
    string OrderNumber,
    string CustomerId,
    DateTimeOffset LastModified,
    IEnumerable<OrderItem> Items);

public record OrderItem(
    string ProductId,
    string ProductName,
    int Quantity);
```

## Event definitions

```csharp
[EventType]
public record UserCreated(string Name, string Email);

[EventType]
public record UserEmailChanged(string UserId, string NewEmail);

[EventType]
public record UserNameChanged(string UserId, string NewName);

[EventType]
public record OrderCreated(string OrderNumber, string CustomerId);

[EventType]
public record ItemAddedToOrder(string OrderId, string ProductId, string ProductName, int Quantity);

[EventType]
public record ItemQuantityChanged(string OrderId, string ProductId, int NewQuantity);
```

## Common use cases

### Audit tracking

Set `LastUpdated`, `ModifiedBy`, or `LastEventSequence` for all events to track when and by whom changes were made.

### Caching and invalidation

Update cache keys or version numbers for all events to support cache invalidation strategies.

### Debugging and monitoring

Track correlation IDs or sequence numbers for all events to aid in debugging and monitoring.

### Event Source identification

Set the event source identifier for the event source for all events if the primary identifier of a read model is different than the `EventSourceId`
but you still want to have a property with which event source it is for.

The `FromEvery()` method provides a powerful way to maintain common properties across all events in a projection, ensuring consistency and reducing duplication in your projection definitions.
