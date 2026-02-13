# Getting Snapshots

Chronicle provides the ability to retrieve snapshots of a read model's state at different points in time, grouped by correlation ID. This feature is invaluable for debugging, auditing, and understanding how a read model evolved through a sequence of related events.

## Overview

Snapshots capture the state of a read model after processing groups of events that share the same correlation ID. Each snapshot includes:

- The read model instance at that point in time
- The events that were applied to create that snapshot
- The timestamp when the first event in the group occurred
- The correlation ID that links the events together

This allows you to see how a read model was built up through different operations or transactions, where each operation is identified by its correlation ID.

## Basic Usage

### Retrieving Snapshots

Use the `GetSnapshotsById` method on the `IReadModels` API:

```csharp
public class OrderAnalysisService
{
    readonly IEventStore _eventStore;

    public OrderAnalysisService(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<IEnumerable<ReadModelSnapshot<Order>>> GetOrderHistory(Guid orderId)
    {
        return await _eventStore.ReadModels.GetSnapshotsById<Order>(orderId);
    }
}
```

### The ReadModelSnapshot Structure

Each snapshot contains:

```csharp
public record ReadModelSnapshot<TReadModel>(
    TReadModel Instance,                    // The complete read model at that point
    IEnumerable<AppendedEvent> Events,      // Events that were applied
    DateTimeOffset Occurred,                // When the first event occurred
    CorrelationId CorrelationId);           // Links related events together
```

## Understanding Correlation ID Grouping

Snapshots are grouped by correlation ID, which means each snapshot represents the cumulative effect of all events that share the same correlation ID. This typically corresponds to:

- A single user request or API call
- A business transaction
- A batch operation
- A scheduled job execution

When you append events to Chronicle, they are automatically tagged with a correlation ID. Related events that are appended together typically share the same correlation ID.

## Example: Order Processing Timeline

Consider an order that goes through several stages:

```csharp
// Initial order creation (Correlation ID: A)
await _eventStore.EventLog.Append(orderId, new OrderCreated(orderId, customerId));
await _eventStore.EventLog.Append(orderId, new OrderItemAdded(productId, quantity));

// Payment processing (Correlation ID: B)
await _eventStore.EventLog.Append(orderId, new PaymentReceived(amount));
await _eventStore.EventLog.Append(orderId, new OrderConfirmed());

// Shipping (Correlation ID: C)
await _eventStore.EventLog.Append(orderId, new OrderShipped(trackingNumber));

// Retrieve all snapshots
var snapshots = await _eventStore.ReadModels.GetSnapshotsById<Order>(orderId);

// You'll get 3 snapshots:
// 1. After creation (events from correlation A)
// 2. After payment (events from correlation B)
// 3. After shipping (events from correlation C)
```

Each snapshot shows the order's state after processing the events from one correlation group.

## Use Cases

### Debugging Read Model Issues

When investigating why a read model has a particular state, retrieve all snapshots to see how it evolved:

```csharp
public async Task DiagnoseOrderState(Guid orderId)
{
    var snapshots = await _eventStore.ReadModels.GetSnapshotsById<Order>(orderId);

    foreach (var snapshot in snapshots)
    {
        Console.WriteLine($"Snapshot at {snapshot.Occurred}:");
        Console.WriteLine($"  Correlation ID: {snapshot.CorrelationId}");
        Console.WriteLine($"  State: {snapshot.Instance}");
        Console.WriteLine($"  Event count: {snapshot.Events.Count()}");
    }
}
```

### Auditing and Compliance

Track state changes for compliance purposes:

```csharp
public async Task<List<StateChange>> GetStateChangeHistory<T>(ReadModelKey key)
{
    var snapshots = await _eventStore.ReadModels.GetSnapshotsById<T>(key);

    return snapshots
        .Select(s => new StateChange
        {
            Timestamp = s.Occurred,
            CorrelationId = s.CorrelationId,
            EventCount = s.Events.Count(),
            State = s.Instance
        })
        .ToList();
}
```

### Understanding Transaction Boundaries

Identify which events were part of the same logical transaction:

```csharp
public async Task AnalyzeTransactionGroups(Guid accountId)
{
    var snapshots = await _eventStore.ReadModels.GetSnapshotsById<Account>(accountId);

    var transactions = snapshots.Select((s, i) => new
    {
        TransactionNumber = i + 1,
        CorrelationId = s.CorrelationId,
        EventIds = s.Events.Select(e => e.EventId),
        ResultingBalance = s.Instance.Balance,
        OccurredAt = s.Occurred
    });

    foreach (var transaction in transactions)
    {
        Console.WriteLine($"Transaction {transaction.TransactionNumber}: {transaction.CorrelationId}");
        Console.WriteLine($"  Events: {string.Join(", ", transaction.EventIds)}");
        Console.WriteLine($"  Balance: {transaction.ResultingBalance:C}");
    }
}
```

### Comparative Analysis

Compare read model state between different snapshots:

```csharp
public async Task CompareSnapshots(Guid key)
{
    var snapshots = await _eventStore.ReadModels.GetSnapshotsById<Order>(key);
    var snapshotList = snapshots.ToList();

    if (snapshotList.Count < 2) return;

    for (int i = 0; i < snapshotList.Count - 1; i++)
    {
        var before = snapshotList[i].Instance;
        var after = snapshotList[i + 1].Instance;

        Console.WriteLine($"Changes between snapshot {i} and {i + 1}:");
        // Compare properties and log differences
    }
}
```

## Performance Considerations

### Snapshot Retrieval Cost

Getting snapshots for a read model involves:

1. Reading all events for the specified key
2. Grouping them by correlation ID
3. Replaying events up to each correlation boundary
4. Building snapshots at each boundary

This can be expensive for read models with:

- Very long event histories
- Many correlation groups
- Complex projection logic

### When to Use GetSnapshotsById

Use snapshots when:

- You need to understand the evolution of a specific instance
- You're debugging or investigating issues
- You're performing auditing or compliance tasks
- The read model key has a reasonable event history

Avoid snapshots when:

- You only need the current state (use `GetInstanceById` instead)
- You need to query all instances (use `GetInstances` instead)
- You need real-time updates (use `Watch` instead)

## Best Practices

### Cache Snapshot Results

Since snapshots are immutable historical data, you can safely cache them:

```csharp
public class SnapshotCache
{
    readonly IEventStore _eventStore;
    readonly Dictionary<ReadModelKey, List<ReadModelSnapshot<Order>>> _cache = new();

    public async Task<IEnumerable<ReadModelSnapshot<Order>>> GetSnapshots(ReadModelKey key)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var snapshots = (await _eventStore.ReadModels.GetSnapshotsById<Order>(key)).ToList();
        _cache[key] = snapshots;

        return snapshots;
    }
}
```

### Combine with Current State

Get both historical snapshots and current state for complete analysis:

```csharp
public async Task<OrderAnalysis> AnalyzeOrder(Guid orderId)
{
    var snapshots = await _eventStore.ReadModels.GetSnapshotsById<Order>(orderId);
    var currentState = await _eventStore.ReadModels.GetInstanceById<Order>(orderId);

    return new OrderAnalysis
    {
        HistoricalSnapshots = snapshots,
        CurrentState = currentState,
        TotalSnapshots = snapshots.Count()
    };
}
```

## Related Topics

- [Getting a Single Instance](getting-single-instance.md) - Retrieve the current state of a specific instance
- [Getting a Collection of Instances](getting-collection-instances.md) - Retrieve all instances of a read model
- [Watching Read Models](watching-read-models.md) - Real-time notifications for read model changes
- [Snapshots concept](snapshots.md) - Conceptual overview of read model snapshots
