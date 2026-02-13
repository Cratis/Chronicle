# Event Processing

Understanding how reducers process events is crucial for building effective read models. This guide covers the event processing model, method patterns, and advanced techniques.

## Event Processing Model

### Event Method Discovery

Reducers use convention-based method discovery. Chronicle automatically finds and invokes methods that:

- Match the event type name (e.g., `OnOrderCreated` for `OrderCreated` events)
- Accept the event type as the first parameter
- Accept the current read model state (nullable) as the second parameter
- Optionally accept `EventContext` as the third parameter

```csharp
public record OrderSummary(Guid OrderId, decimal Total, DateTimeOffset LastUpdated);

public class OrderSummaryReducer : IReducerFor<OrderSummary>
{
    public OrderSummary OnOrderCreated(OrderCreated @event, OrderSummary? current, EventContext context)
    {
        return new OrderSummary(@event.OrderId, 0m, context.Occurred);
    }

    public OrderSummary OnItemAdded(ItemAdded @event, OrderSummary? current, EventContext context)
    {
        if (current is null) return null!; // Skip if no order exists

        return current with
        {
            Total = current.Total + @event.Price,
            LastUpdated = context.Occurred
        };
    }
}
```

### Event Source Isolation

Each reducer method is called for a single event source:

- Events for the same event source (e.g., the same order) are processed sequentially
- The `current` parameter contains the current state for that specific event source
- Each event source has its own independent state

### Sequential Processing

Events are guaranteed to be processed in sequence order:

1. Events are ordered by their sequence number
2. Each method is called once per event
3. The return value becomes the `current` parameter for the next event

## Method Signatures

### Basic Synchronous Pattern

The simplest pattern accepts the event and current state:

```csharp
public TReadModel OnEventName(EventType @event, TReadModel? current)
{
    // Process event and return new state
    return newState;
}
```

### Pattern with Event Context

Access event metadata by adding `EventContext` parameter:

```csharp
public TReadModel OnEventName(EventType @event, TReadModel? current, EventContext context)
{
    // Access occurred time, correlation ID, etc.
    return newState;
}
```

### Async Patterns

Both patterns support async methods:

```csharp
// Async without context
public Task<TReadModel> OnEventName(EventType @event, TReadModel? current)
{
    return Task.FromResult(newState);
}

// Async with context
public async Task<TReadModel> OnEventName(EventType @event, TReadModel? current, EventContext context)
{
    // Perform async operations
    return await ComputeStateAsync(@event, current);
}
```

## Event Context

The `EventContext` provides metadata about the event:

```csharp
public record EventContext
{
    public EventSequenceNumber SequenceNumber { get; }
    public EventSourceId EventSourceId { get; }
    public EventType EventType { get; }
    public DateTimeOffset Occurred { get; }
    public CorrelationId CorrelationId { get; }
    public CausationId CausationId { get; }
    public Identity CausedBy { get; }
    // ... and more
}
```

### Using Event Context

```csharp
public OrderSummary OnOrderPlaced(OrderPlaced @event, OrderSummary? current, EventContext context)
{
    return new OrderSummary(
        OrderId: @event.OrderId,
        Total: @event.Amount,
        PlacedAt: context.Occurred,
        PlacedBy: context.CausedBy.ToString(),
        CorrelationId: context.CorrelationId);
}
```

## Current State Parameter

The `current` parameter represents the previously computed state for this event source.

### First Event

When processing the first event for an event source:

- `current` is `null`
- Initialize your read model with appropriate values
- You can return `null!` to skip creating state for certain events

```csharp
public Analytics OnDataRecorded(DataRecorded @event, Analytics? current, EventContext context)
{
    if (current is null)
    {
        // First event - initialize state
        return new Analytics(
            EventCount: 1,
            FirstEventTime: context.Occurred,
            LastEventTime: context.Occurred,
            TotalValue: @event.Value);
    }

    // Update existing state
    return current with
    {
        EventCount = current.EventCount + 1,
        LastEventTime = context.Occurred,
        TotalValue = current.TotalValue + @event.Value
    };
}
```

### Subsequent Events

For subsequent events:

- `current` contains the state from the previous event
- Use record's `with` expression to create modified copies
- Return the new state

## Processing Patterns

### Pattern 1: Accumulation

Accumulate values across events:

```csharp
public record Statistics(decimal Sum, int Count, decimal Average);

public class StatisticsReducer : IReducerFor<Statistics>
{
    public Statistics OnMetricRecorded(MetricRecorded @event, Statistics? current)
    {
        var sum = (current?.Sum ?? 0) + @event.Value;
        var count = (current?.Count ?? 0) + 1;

        return new Statistics(sum, count, sum / count);
    }
}
```

### Pattern 2: State Transitions

Track state changes through events:

```csharp
public record OrderStatus(string State, DateTimeOffset LastUpdated);

public class OrderStatusReducer : IReducerFor<OrderStatus>
{
    public OrderStatus OnOrderCreated(OrderCreated @event, OrderStatus? current, EventContext context)
        => new OrderStatus("Created", context.Occurred);

    public OrderStatus OnOrderPaid(OrderPaid @event, OrderStatus? current, EventContext context)
        => new OrderStatus("Paid", context.Occurred);

    public OrderStatus OnOrderShipped(OrderShipped @event, OrderStatus? current, EventContext context)
        => new OrderStatus("Shipped", context.Occurred);

    public OrderStatus OnOrderDelivered(OrderDelivered @event, OrderStatus? current, EventContext context)
        => new OrderStatus("Delivered", context.Occurred);

    public OrderStatus OnOrderCancelled(OrderCancelled @event, OrderStatus? current, EventContext context)
        => new OrderStatus("Cancelled", context.Occurred);
}
```

### Pattern 3: Collection Building

Build collections from events:

```csharp
public record Activity(string Type, DateTimeOffset Timestamp, string Description);
public record CustomerActivityLog(List<Activity> Activities);

public class CustomerActivityLogReducer : IReducerFor<CustomerActivityLog>
{
    public CustomerActivityLog OnCustomerAction(CustomerAction @event, CustomerActivityLog? current, EventContext context)
    {
        var activities = current?.Activities ?? new List<Activity>();

        activities.Add(new Activity(
            @event.Type,
            context.Occurred,
            @event.Description));

        return new CustomerActivityLog(activities);
    }
}
```

### Pattern 4: Time-Based Aggregation

Aggregate events within time windows:

```csharp
public record HourlyMetrics(Dictionary<int, decimal> MetricsByHour);

public class HourlyMetricsReducer : IReducerFor<HourlyMetrics>
{
    public HourlyMetrics OnMetricRecorded(MetricRecorded @event, HourlyMetrics? current, EventContext context)
    {
        var metricsByHour = current?.MetricsByHour ?? new Dictionary<int, decimal>();
        var hour = context.Occurred.Hour;

        if (!metricsByHour.ContainsKey(hour))
            metricsByHour[hour] = 0;

        metricsByHour[hour] += @event.Value;

        return new HourlyMetrics(metricsByHour);
    }
}
```

### Pattern 5: Conditional Processing

Skip processing based on conditions:

```csharp
public record Account(Guid AccountId, decimal Balance, bool IsActive);

public class AccountReducer : IReducerFor<Account>
{
    public Account OnAccountOpened(AccountOpened @event, Account? current, EventContext context)
    {
        return new Account(@event.AccountId, 0m, true);
    }

    public Account OnDepositMade(DepositMade @event, Account? current, EventContext context)
    {
        // Skip if account doesn't exist or is not active
        if (current is null || !current.IsActive) return null!;

        return current with { Balance = current.Balance + @event.Amount };
    }

    public Account OnAccountClosed(AccountClosed @event, Account? current, EventContext context)
    {
        if (current is null) return null!;

        return current with { IsActive = false };
    }
}
```

## Error Handling

### Handling Invalid State

Return `null!` to skip creating/updating state:

```csharp
public OrderSummary OnItemAdded(ItemAdded @event, OrderSummary? current, EventContext context)
{
    // Can't add items if order doesn't exist
    if (current is null) return null!;

    return current with
    {
        Total = current.Total + @event.Price
    };
}
```

### Recording Errors in State

Include error information in your read model:

```csharp
public record ValidationResult(bool IsValid, List<string> Errors);

public class ValidationResultReducer : IReducerFor<ValidationResult>
{
    public ValidationResult OnInvalidDataDetected(InvalidDataDetected @event, ValidationResult? current)
    {
        var errors = current?.Errors ?? new List<string>();
        errors.Add(@event.Reason);

        return new ValidationResult(false, errors);
    }
}
```

## Performance Optimization

### Minimize Object Creation

Leverage record types with `with` expressions:

```csharp
// Efficient - only creates new object when needed
public Stats OnMetricRecorded(MetricRecorded @event, Stats? current)
{
    if (current is null)
        return new Stats(Count: 1, Sum: @event.Value);

    return current with
    {
        Count = current.Count + 1,
        Sum = current.Sum + @event.Value
    };
}
```

### Reuse Collections

Be mindful of collection modifications:

```csharp
public record ItemList(List<Item> Items);

public ItemList OnItemAdded(ItemAdded @event, ItemList? current)
{
    // Reuse existing list
    var items = current?.Items ?? new List<Item>();
    items.Add(new Item(@event.ItemId, @event.Name));

    return new ItemList(items);
}
```

## Best Practices

1. **Use record types** - Prefer immutable record types for read models
2. **Keep logic pure** - Avoid side effects; only compute state from events
3. **Handle null safely** - Always check `current` for null on first event
4. **Use with expressions** - Leverage record's `with` for clean state updates
5. **Return null! to skip** - Use `null!` to skip creating/updating state
6. **Access context when needed** - Use `EventContext` for metadata like timestamps
7. **Name methods clearly** - Use descriptive method names that match event types
8. **Test thoroughly** - Unit test with various event sequences and edge cases


