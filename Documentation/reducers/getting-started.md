# Getting Started with Reducers

Reducers provide a powerful way to build read models by reducing a sequence of events into aggregated state. This guide will walk you through creating your first reducer.

## Prerequisites

Before you begin, ensure you have:

- A Chronicle-enabled application
- Basic understanding of events and event sourcing
- A read model class to reduce events into

## Creating a Reducer

### 1. Define Your Read Model

First, create a record representing the state you want to compute:

```csharp
public record OrderSummary(
    Guid OrderId,
    decimal TotalAmount,
    int ItemCount,
    DateTimeOffset LastUpdated);
```

### 2. Implement the Reducer

Create a reducer by implementing `IReducerFor<TReadModel>` with methods for each event type:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public class OrderSummaryReducer : IReducerFor<OrderSummary>
{
    public OrderSummary OnOrderCreated(OrderCreated @event, OrderSummary? current, EventContext context)
    {
        return new OrderSummary(
            OrderId: @event.OrderId,
            TotalAmount: 0m,
            ItemCount: 0,
            LastUpdated: context.Occurred);
    }

    public OrderSummary OnItemAddedToOrder(ItemAddedToOrder @event, OrderSummary? current, EventContext context)
    {
        if (current is null) return null!; // Skip if order not created yet

        return current with
        {
            TotalAmount = current.TotalAmount + (@event.Price * @event.Quantity),
            ItemCount = current.ItemCount + @event.Quantity,
            LastUpdated = context.Occurred
        };
    }

    public OrderSummary OnItemRemovedFromOrder(ItemRemovedFromOrder @event, OrderSummary? current, EventContext context)
    {
        if (current is null) return null!; // Skip if order not created yet

        return current with
        {
            TotalAmount = current.TotalAmount - (@event.Price * @event.Quantity),
            ItemCount = current.ItemCount - @event.Quantity,
            LastUpdated = context.Occurred
        };
    }
}
```

## Method Signatures

Reducer methods are discovered by convention and support the following signatures:

### Synchronous Methods

```csharp
// Without context
public MyReadModel MethodName(MyEvent @event, MyReadModel? current);

// With context
public MyReadModel MethodName(MyEvent @event, MyReadModel? current, EventContext context);
```

### Asynchronous Methods

```csharp
// Without context
public Task<MyReadModel> MethodName(MyEvent @event, MyReadModel? current);

// With context
public Task<MyReadModel> MethodName(MyEvent @event, MyReadModel? current, EventContext context);
```

**Key Points:**

- Method names can be anything, but typically start with `On` followed by the event type name
- The `@event` parameter is the specific event being processed
- The `current` parameter contains the existing state (null if no previous state exists)
- The `EventContext` parameter provides metadata like event source ID, occurred timestamp, and sequence number
- Both synchronous and asynchronous methods are supported

### 3. Using the Reducer Attribute (Optional)

You can customize the reducer using the `[Reducer]` attribute:

```csharp
[Reducer(id: "order-summary", eventSequence: "order-events")]
public class OrderSummaryReducer : IReducerFor<OrderSummary>
{
    // Implementation
}
```

**Attribute parameters:**

- `id` - Custom identifier for the reducer (defaults to the fully qualified type name)
- `eventSequence` - The event sequence to observe (defaults to the event log)
- `isActive` - Whether the reducer actively observes events (defaults to `true`)

## Retrieving Reduced State

Once your reducer is set up, you can retrieve the computed state:

```csharp
public class OrderService
{
    readonly IReducers _reducers;

    public OrderService(IReducers reducers)
    {
        _reducers = reducers;
    }

    public async Task<OrderSummary> GetOrderSummary(Guid orderId)
    {
        var result = await _reducers.GetInstanceById<OrderSummary>(orderId);
        return result.ReadModel;
    }
}
```

## Event Processing

Reducer methods are called for each event matching the event type:

1. **Event Type Matching** - Chronicle calls the method that handles the specific event type
2. **Event Source ID** - Each method receives events for a single event source
3. **Sequential Processing** - Events are processed in the order they were appended

The `current` parameter in each method:

- Is `null` when no previous state exists for this event source
- Contains the current persisted state for this event source

## Best Practices

1. **Keep reducers pure** - Avoid side effects; only compute state from events
2. **Handle null current state** - Always check if `current` is null for the initial state
3. **Use immutable state** - Create new instances rather than mutating the current state
4. **Process events in order** - The events are provided in sequence order; respect that order
5. **Consider performance** - For large event streams, optimize your reduction logic

## Next Steps

- Learn about [Passive Reducers](passive-reducers.md) to control when reducers observe
- Explore [Event Processing](event-processing.md) for advanced patterns
