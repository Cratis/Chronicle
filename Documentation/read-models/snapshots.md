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
        Console.WriteLine($"Correlation: {snapshot.CorrelationId}");
        Console.WriteLine($"Occurred: {snapshot.Occurred}");
        Console.WriteLine($"Events: {snapshot.Events.Count()}");
        Console.WriteLine($"Order Total: {snapshot.Instance.TotalAmount}");
        Console.WriteLine("---");
    }
}
```

### Audit Trail

Create an audit log showing how a read model changed through different operations:

```csharp
public async Task<AuditTrail> GetAuditTrail(Guid accountId)
{
    var snapshots = await _eventStore.ReadModels.GetSnapshotsById<Account>(accountId);

    return new AuditTrail
    {
        AccountId = accountId,
        Changes = snapshots.Select(snapshot => new AuditEntry
        {
            CorrelationId = snapshot.CorrelationId,
            Timestamp = snapshot.Occurred,
            Operations = snapshot.Events.Select(e => e.Content.GetType().Name).ToList(),
            ResultingBalance = snapshot.Instance.Balance
        }).ToList()
    };
}
```

### Compliance and Regulatory Reporting

Generate detailed reports showing the complete history of changes:

```csharp
public async Task<ComplianceReport> GenerateComplianceReport(Guid accountId)
{
    var snapshots = await _eventStore.ReadModels.GetSnapshotsById<Account>(accountId);

    return new ComplianceReport
    {
        AccountId = accountId,
        Transactions = snapshots.Select((snapshot, index) => new TransactionRecord
        {
            SequenceNumber = index + 1,
            Timestamp = snapshot.Occurred,
            CorrelationId = snapshot.CorrelationId,
            EventTypes = snapshot.Events.Select(e => e.Content.GetType().Name).ToList(),
            BalanceAfter = snapshot.Instance.Balance,
            BalanceBefore = index > 0
                ? snapshots.ElementAt(index - 1).Instance.Balance
                : 0m
        }).ToList()
    };
}
```

### Testing and Verification

Verify that a series of events produces the expected read model state:

```csharp
[Fact]
public async Task should_calculate_correct_balance_after_multiple_operations()
{
    // Arrange
    var accountId = Guid.NewGuid();
    await AppendTestEvents(accountId);

    // Act
    var snapshots = await _eventStore.ReadModels.GetSnapshotsById<Account>(accountId);
    var finalSnapshot = snapshots.Last();

    // Assert
    finalSnapshot.Instance.Balance.ShouldEqual(expectedBalance);
    finalSnapshot.Events.Count().ShouldEqual(expectedEventCount);
}
```

### Temporal Queries

Answer questions about the state at specific points in time:

```csharp
public async Task<Account?> GetAccountStateAtTime(Guid accountId, DateTimeOffset targetTime)
{
    var snapshots = await _eventStore.ReadModels.GetSnapshotsById<Account>(accountId);

    // Find the last snapshot that occurred before or at the target time
    return snapshots
        .Where(s => s.Occurred <= targetTime)
        .OrderByDescending(s => s.Occurred)
        .FirstOrDefault()?.Instance;
}
```

## Working with Snapshots

### Iterating Through History

Process each snapshot to build a timeline:

```csharp
public async Task<Timeline> BuildTimeline(Guid entityId)
{
    var snapshots = await _eventStore.ReadModels.GetSnapshotsById<Entity>(entityId);
    var timeline = new Timeline();

    foreach (var snapshot in snapshots)
    {
        timeline.AddEntry(new TimelineEntry
        {
            When = snapshot.Occurred,
            What = string.Join(", ", snapshot.Events.Select(e => e.Content.GetType().Name)),
            State = snapshot.Instance
        });
    }

    return timeline;
}
```

### Comparing States

Compare snapshots to understand what changed:

```csharp
public async Task<List<Change>> GetChanges(Guid accountId)
{
    var snapshots = await _eventStore.ReadModels.GetSnapshotsById<Account>(accountId);
    var changes = new List<Change>();

    Account? previous = null;
    foreach (var snapshot in snapshots)
    {
        if (previous != null)
        {
            changes.Add(new Change
            {
                From = previous.Balance,
                To = snapshot.Instance.Balance,
                Delta = snapshot.Instance.Balance - previous.Balance,
                When = snapshot.Occurred,
                CorrelationId = snapshot.CorrelationId
            });
        }
        previous = snapshot.Instance;
    }

    return changes;
}
```

## Snapshots for Different Read Model Types

### Projection-Based Read Models

Snapshots work seamlessly with both model-bound and declarative projections:

```csharp
// Works with model-bound projections
var projectionsSnapshots = await _eventStore.ReadModels.GetSnapshotsById<AccountInfo>(accountId);

// Works with declarative projections
var declarativeSnapshots = await _eventStore.ReadModels.GetSnapshotsById<OrderSummary>(orderId);
```

### Reducer-Based Read Models

Snapshots also work with reducers:

```csharp
// Works with reducers
var reducerSnapshots = await _eventStore.ReadModels.GetSnapshotsById<ShoppingCart>(cartId);

foreach (var snapshot in reducerSnapshots)
{
    Console.WriteLine($"Cart had {snapshot.Instance.Items.Count} items");
    Console.WriteLine($"Total: {snapshot.Instance.Total}");
}
```

## Performance Considerations

### Event Replay Cost

Getting snapshots requires replaying events from the beginning of the event log for the specified read model key. Consider these factors:

- **Number of events**: More events take longer to replay
- **Complexity of logic**: Complex projections/reducers take longer to compute
- **Frequency of access**: Frequent snapshot retrieval can impact performance

### Best Practices

1. **Use for diagnostics, not regular operations**: Snapshots are designed for analytical and debugging purposes, not for regular application logic
2. **Consider caching**: If you need to access snapshots repeatedly in a short time, cache the results
3. **Limit scope**: Only request snapshots when necessary, not as part of routine operations
4. **Filter in application**: If you only need recent snapshots, retrieve all and filter client-side rather than making multiple calls

### When to Use Alternatives

For regular read model access, use:
- `GetInstanceById` for current state with strong consistency
- Materialized projections for frequently accessed data
- Projection watchers for real-time updates

## Snapshot Ordering

Snapshots are returned in chronological order based on the timestamp of the first event in each correlation group. This provides a natural timeline view of how the read model evolved.

```csharp
var snapshots = await _eventStore.ReadModels.GetSnapshotsById<Account>(accountId);

// First snapshot is the oldest
var firstChange = snapshots.First();
Console.WriteLine($"Account created at: {firstChange.Occurred}");

// Last snapshot is the most recent
var latestChange = snapshots.Last();
Console.WriteLine($"Latest change at: {latestChange.Occurred}");
```

## Related Topics

- [Getting Instances](getting-instances.md) - Learn how to retrieve current read model state
- [Projections](../projections/index.md) - Learn more about defining projections
- [Reducers](../reducers/index.md) - Learn more about defining reducers
- [Event Metadata and Tags](../concepts/event-metadata-tags.md) - Understanding correlation IDs and event metadata
