# Reducers

Reducers are specialized observers in Chronicle that automatically create and maintain read models by reacting to events. They provide a declarative way to build the current state from event streams without having to manually manage database operations.

## Purpose

Reducers serve as the bridge between your event-driven domain and read models. Unlike [reactors](./reacting-events-in-event-log.md) which are designed for side effects and general event handling, reducers are specifically optimized for:

- **State Aggregation**: Building current state from historical events
- **Read Model Creation**: Automatically persisting computed state to the database
- **Event Replay**: Rebuilding state when events are replayed
- **Simplified Database Management**: Chronicle handles all database operations for you

## How Reducers Work

When events are appended to the event log, Chronicle automatically:

1. **Discovers** reducer methods that handle specific event types
2. **Invokes** the appropriate reducer method with the event and current state
3. **Persists** the returned state to the configured database
4. **Manages** concurrency and error handling

## Asynchronous Processing and Eventual Consistency

Reducers operate under **eventual consistency** principles, meaning they process events asynchronously after they are stored in the event log. This design provides several important characteristics:

### Asynchronous Event Processing

When you append an event to Chronicle:

1. **Event is persisted** to the event store immediately
2. **Append operation returns** successfully to the caller
3. **Reducers are executed asynchronously** in the background
4. **Read models are updated** after processing completes

This means there's a brief window where the event has been stored but reducers may not yet have processed it.

### Benefits of Asynchronous Processing

- **High Performance**: Event appends don't wait for reducer execution
- **Scalability**: Reducer processing can be scaled independently from event storage
- **Resilience**: Failed reducer execution doesn't affect event persistence
- **Consistency**: Events for the same event source are processed in order

### Implications for Application Design

When designing applications with reducers, consider:

- **Immediate Reads**: Read models may not immediately reflect just-appended events
- **Fire-and-Forget Pattern**: Commands should append events and return, not expect immediate consistency
- **UI Updates**: Consider optimistic UI updates or event-driven refresh patterns

```csharp
// ✅ Good - Fire and forget
public async Task<IActionResult> UpdateBookQuantity(UpdateQuantityCommand command)
{
    await eventStore.EventLog.Append(command.BookId, new BookQuantityUpdated(command.NewQuantity));
    return Ok(); // Don't try to read updated state immediately
}

// ❌ Problematic - Expecting immediate consistency
public async Task<BookInventory> UpdateAndReturn(UpdateQuantityCommand command)
{
    await eventStore.EventLog.Append(command.BookId, new BookQuantityUpdated(command.NewQuantity));

    // This may return stale data due to eventual consistency
    return await readModels.GetBook(command.BookId);
}
```

### Step 1: Define a Read Model

First, create a class or record that represents the state you want to maintain:

```csharp
public record BookInventory(
    Guid Id,
    string Title,
    string Author,
    int Quantity,
    decimal Price,
    DateTime LastUpdated);
```

### Step 2: Implement the Reducer

Create a reducer class that implements `IReducerFor<TReadModel>`:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public class BookInventoryReducer : IReducerFor<BookInventory>
{
    public BookInventory OnBookAdded(BookAddedToInventory @event, BookInventory? current, EventContext context)
    {
        return new BookInventory(
            Id: context.EventSourceId.Value,
            Title: @event.Title,
            Author: @event.Author,
            Quantity: @event.InitialQuantity,
            Price: @event.Price,
            LastUpdated: context.Occurred);
    }

    public BookInventory OnQuantityUpdated(BookQuantityUpdated @event, BookInventory? current, EventContext context)
    {
        if (current is null) return null!; // Skip if no existing state

        return current with
        {
            Quantity = @event.NewQuantity,
            LastUpdated = context.Occurred
        };
    }

    public BookInventory OnPriceChanged(BookPriceChanged @event, BookInventory? current, EventContext context)
    {
        if (current is null) return null!; // Skip if no existing state

        return current with
        {
            Price = @event.NewPrice,
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

## Key Concepts

### Current State Parameter

The `current` parameter contains the existing state from the database:

- **Null**: No previous state exists (first time processing for this event source)
- **Non-null**: Current state that can be updated

### Event Context

The `EventContext` provides additional information about the event:

- `EventSourceId`: The unique identifier for the entity the event applies to
- `Occurred`: When the event occurred
- `SequenceNumber`: The position of the event in the sequence

### Return Values

- **Return new/updated state**: Chronicle will persist the returned object
- **Return null**: Chronicle will skip persistence for this event
- **Throw exception**: The reducer will be marked as failed for this partition

## Error Handling

If a reducer method throws an exception:

1. The **partition** (EventSourceId) is marked as failed
2. No further events for that partition are processed
3. Chronicle automatically retries failed partitions on a schedule
4. Once resolved, processing continues from where it left off

## Managing Reducer State

### Getting Reducer State

```csharp
// Get the current state of a specific reducer
var state = await reducers.GetStateFor<BookInventoryReducer>();
```

### Replaying Events

```csharp
// Replay all events for a reducer (rebuilds read models)
await reducers.Replay<BookInventoryReducer>();

// Replay by reducer ID
await reducers.Replay(reducerId);
```

## Best Practices

### 1. Keep Reducers Pure

Reducers should be pure functions that only depend on the event and current state:

```csharp
// ✅ Good - Pure function
public BookInventory OnBookAdded(BookAddedToInventory @event, BookInventory? current, EventContext context)
{
    return new BookInventory(context.EventSourceId.Value, @event.Title, @event.Author);
}

// ❌ Avoid - Side effects
public BookInventory OnBookAdded(BookAddedToInventory @event, BookInventory? current, EventContext context)
{
    // Don't call external services
    _emailService.SendNotification("Book added");
    return new BookInventory(context.EventSourceId.Value, @event.Title, @event.Author);
}
```

### 2. Handle Null State Gracefully

Always check for null current state when events might be processed out of order:

```csharp
public BookInventory OnQuantityUpdated(BookQuantityUpdated @event, BookInventory? current, EventContext context)
{
    // Handle case where update event arrives before creation event
    if (current is null)
    {
        // Could return null to skip, or create a minimal state
        return null!;
    }

    return current with { Quantity = @event.NewQuantity };
}
```

### 3. Use Immutable Read Models

Prefer records or immutable classes for read models:

```csharp
// ✅ Good - Immutable record
public record BookInventory(Guid Id, string Title, int Quantity);

// ❌ Avoid - Mutable class
public class BookInventory
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public int Quantity { get; set; }
}
```

### 4. Single Responsibility

Each reducer should focus on a single read model or bounded context:

```csharp
// ✅ Good - Focused on book inventory
public class BookInventoryReducer : IReducerFor<BookInventory> { }

// ✅ Good - Separate reducer for analytics
public class BookAnalyticsReducer : IReducerFor<BookAnalytics> { }
```

## Discovery and Registration

Reducers are automatically discovered at startup when they:

- Implement `IReducerFor<TReadModel>`
- Are registered in the dependency injection container
- Have public methods that match the expected signatures

Chronicle handles all the infrastructure concerns, allowing you to focus purely on the business logic of state transformation.

## Comparison with Reactors

| Aspect | Reducers | Reactors |
|--------|----------|----------|
| **Purpose** | Create and maintain read models | Perform side effects and general event handling |
| **Database** | Automatically managed by Chronicle | Manual database operations |
| **State** | Maintains current state | Stateless operations |
| **Return Value** | Returns new state to persist | No return value (void/Task) |
| **Use Cases** | Read models, aggregated views | Notifications, integrations, workflows |

Choose reducers when you need to build and maintain current state from events. Choose reactors when you need to perform side effects or integrate with external systems.
