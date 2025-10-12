# Projection with FromEventSequence

The `FromEventSequence()` method allows you to specify which event sequence a projection should source events from. This is useful when you have multiple event sequences in your system and want to create projections that only process events from specific sequences.

## Defining a projection with specific event sequence

Use `FromEventSequence()` to specify the event sequence to source events from:

```csharp
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.EventSequences;

public class OrderProjection : IProjectionFor<Order>
{
    public void Define(IProjectionBuilderFor<Order> builder) => builder
        .FromEventSequence("order-management")
        .AutoMap()
        .From<OrderCreated>()
        .From<OrderUpdated>()
        .From<OrderShipped>();
}
```

This projection:

- Only processes events from the "order-management" event sequence
- Ignores events from other sequences like "user-management" or "inventory-management"
- Uses the specified sequence for all event handling

## Event sequence identification

Event sequences can be identified using string names or EventSequenceId:

```csharp
// Using string identifier
.FromEventSequence("order-management")

// Using a constant or configuration value
.FromEventSequence(EventSequences.OrderManagement)
```

## Read model definition

The read model remains the same regardless of the event sequence:

```csharp
public record Order(
    string OrderNumber,
    string CustomerId,
    decimal TotalAmount,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ShippedAt);

public enum OrderStatus
{
    Created,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
```

## Event definitions

Events should be designed to work within the specific sequence context:

```csharp
[EventType]
public record OrderCreated(
    string OrderNumber,
    string CustomerId,
    decimal TotalAmount);

[EventType]
public record OrderUpdated(
    string OrderNumber,
    decimal NewTotalAmount);

[EventType]
public record OrderShipped(
    string OrderNumber,
    DateTimeOffset ShippedAt);
```

## How it works

When using `FromEventSequence()`:

1. The projection subscribes only to the specified event sequence
2. Events from other sequences are ignored, even if they match the event types
3. The projection processes events in the order they appear in the specified sequence
4. Event sequence numbers and ordering are maintained within that specific sequence

## Multiple event sequences

You can create different projections for different event sequences:

```csharp
// Projection for order management events
public class OrderProjection : IProjectionFor<Order>
{
    public void Define(IProjectionBuilderFor<Order> builder) => builder
        .FromEventSequence("order-management")
        .From<OrderCreated>(_ => _
            .Set(m => m.OrderNumber).To(e => e.OrderNumber)
            .Set(m => m.CustomerId).To(e => e.CustomerId)
            .Set(m => m.TotalAmount).To(e => e.TotalAmount)
            .Set(m => m.Status).To(_ => OrderStatus.Created));
}

// Projection for shipping events from a different sequence
public class ShippingProjection : IProjectionFor<Shipping>
{
    public void Define(IProjectionBuilderFor<Shipping> builder) => builder
        .FromEventSequence("shipping-management")
        .From<PackageCreated>()
        .From<PackageShipped>()
        .From<PackageDelivered>();
}
```

## When to use FromEventSequence

Use `FromEventSequence()` when:

- **Bounded contexts**: You have separate domains with their own event sequences
- **Data partitioning**: Events are logically separated by business area or tenant
- **Security boundaries**: Different sequences have different access requirements
- **Performance optimization**: You want to reduce the number of events a projection processes
- **Legacy integration**: You need to process events from specific legacy systems
- **Multi-tenant scenarios**: Each tenant has their own event sequence

## Default behavior

If you don't specify `FromEventSequence()`:

- The projection uses the default `event-log` event sequence.
- All events matching the specified types are processed regardless of sequence
- This is suitable for most single-sequence scenarios

## Performance considerations

- Specifying an event sequence can improve performance by reducing the number of events processed
- Each sequence maintains its own ordering and sequence numbers
- Consider the volume and frequency of events in each sequence when designing projections
- Event sequence isolation can help with parallel processing and scaling
