# Getting Snapshots

Chronicle provides the ability to retrieve snapshots of a projection's state at different points in time, grouped by correlation ID. This feature is useful for debugging, auditing, and understanding how a read model evolved through a sequence of related events.

## Overview

Snapshots capture the state of a projection after processing a group of events that share the same correlation ID. Each snapshot includes:

- The projected read model instance
- The events that were applied to create that snapshot
- The timestamp when the first event in the group occurred
- The correlation ID that links the events together

This allows you to see how a read model was built up through different operations or transactions, where each operation is identified by its correlation ID.

## Accessing the Snapshots API

The snapshots functionality is available through the `IEventStore.Projections` API:

```csharp
public class OrderAnalysisService
{
    private readonly IEventStore _eventStore;

    public OrderAnalysisService(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<IEnumerable<ProjectionSnapshot<Order>>> GetOrderHistory(OrderId orderId)
    {
        var snapshots = await _eventStore.Projections.GetSnapshotsById<Order>(orderId);
        return snapshots;
    }
}
```

## The ProjectionSnapshot Structure

Each snapshot contains the following information:

- **Instance**: The complete read model at that point in time
- **Events**: The collection of events that were applied to reach this state
- **Occurred**: The timestamp when the first event in the correlation group occurred
- **CorrelationId**: The unique identifier linking all events in this operation

## Use Cases

### Debugging Projection Issues

When investigating why a read model has a particular state, you can retrieve all snapshots to see how it evolved:

```csharp
public async Task DiagnoseOrderState(OrderId orderId)
{
    var snapshots = await _eventStore.Projections.GetSnapshotsById<Order>(orderId);

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
public async Task<AuditTrail> GetAuditTrail(AccountId accountId)
{
    var snapshots = await _eventStore.Projections.GetSnapshotsById<Account>(accountId);

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

### Testing and Verification

Verify that a series of events produces the expected read model state:

```csharp
[Fact]
public async Task should_calculate_correct_balance_after_multiple_operations()
{
    var snapshots = await _eventStore.Projections.GetSnapshotsById<Account>(accountId);

    var finalSnapshot = snapshots.Last();

    finalSnapshot.Instance.Balance.ShouldEqual(expectedBalance);
    finalSnapshot.Events.Count().ShouldEqual(expectedEventCount);
}
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
await eventLog.Append(orderId, new OrderCreated(orderId, customerId));
await eventLog.Append(orderId, new OrderItemAdded(productId, quantity));

// Payment processing (Correlation ID: B)
await eventLog.Append(orderId, new PaymentReceived(amount));
await eventLog.Append(orderId, new OrderConfirmed());

// Shipping (Correlation ID: C)
await eventLog.Append(orderId, new OrderShipped(trackingNumber));

// Retrieve all snapshots
var snapshots = await _eventStore.Projections.GetSnapshotsById<Order>(orderId);

// You'll get 3 snapshots:
// 1. After creation (events from correlation A)
// 2. After payment (events from correlation B)
// 3. After shipping (events from correlation C)
```

Each snapshot shows the order's state after processing the events from one correlation group.

## Best Practices

### Use for Diagnostics, Not Regular Operations

Snapshots are designed for diagnostic and analytical purposes. For regular read model access, use standard projection queries or watch functionality.

### Consider Performance

Getting snapshots requires replaying events from the beginning of the event log for the specified read model key. This can be resource-intensive for read models with long event histories.

### Snapshot Ordering

Snapshots are returned in the order they occurred, based on the timestamp of the first event in each correlation group. This provides a chronological view of how the read model evolved.

## Related Topics

- [Watching Projections](watching-projections.md) - Real-time notifications when projections change
- [Model-Bound Projections](model-bound/index.md) - Defining projections using attributes
- [Declarative Projections](declarative/index.md) - Defining projections using a fluent API
