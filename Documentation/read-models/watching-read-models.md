# Watching Read Models

The `IReadModels` API provides the `Watch<TReadModel>()` method to observe real-time changes to read models. This is useful for building reactive user interfaces, triggering background jobs, or maintaining synchronized caches based on read model updates.

## Overview

When you watch a read model using `Watch<T>`, Chronicle:

1. Returns an `IObservable<ReadModelChangeset<T>>` that emits changes as they occur
2. Observes changes from both projections and reducers
3. Provides detailed information about what changed and why
4. Allows you to react immediately to state transitions

This enables reactive patterns where your application can respond instantly to read model changes without polling.

## Basic Usage

### Setting Up Observation

Create an observer for a read model type:

```csharp
public class NotificationService
{
    readonly IEventStore _eventStore;

    public NotificationService(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public void WatchOrders()
    {
        var observable = _eventStore.ReadModels.Watch<Order>();

        subscription = observable.Subscribe(
            changeset =>
            {
                Console.WriteLine($"Order {changeset.ReadModelKey} changed");
                Console.WriteLine($"New state: {changeset.Current}");
            },
            error => Console.WriteLine($"Error: {error}"),
            () => Console.WriteLine("Watch completed")
        );
    }
}
```

### The ReadModelChangeset Structure

Each changeset emitted by the observable contains:

```csharp
public record ReadModelChangeset<TReadModel>(
    ReadModelKey ReadModelKey,              // The key of the changed instance
    TReadModel Current,                     // The current state of the instance
    TReadModel? Previous,                   // The previous state (null on first change)
    IEnumerable<AppendedEvent> Events,      // Events that caused this change
    CorrelationId CorrelationId);           // Links related changes together
```

## Usage Patterns

### WebSocket Updates for Real-Time UI

Push real-time updates to connected clients:

```csharp
public class OrderWatchHub : Hub
{
    readonly IEventStore _eventStore;
    readonly IDisposable _subscription;

    public OrderWatchHub(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task StartWatchingOrders()
    {
        var observable = _eventStore.ReadModels.Watch<Order>();

        _subscription = observable.Subscribe(
            async changeset =>
            {
                await Clients.All.SendAsync("OrderChanged", new
                {
                    orderId = changeset.ReadModelKey,
                    order = changeset.Current,
                    previousState = changeset.Previous
                });
            },
            error => Console.WriteLine($"Error: {error}")
        );
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _subscription?.Dispose();
        await base.OnDisconnectedAsync(exception);
    }
}
```

### Caching Updates

Keep an in-memory cache synchronized with read model changes:

```csharp
public class CachedReadModelService
{
    readonly IEventStore _eventStore;
    readonly Dictionary<ReadModelKey, Account> _cache = new();
    IDisposable? _subscription;

    public void StartCacheSync()
    {
        var observable = _eventStore.ReadModels.Watch<Account>();

        _subscription = observable.Subscribe(
            changeset =>
            {
                _cache[changeset.ReadModelKey] = changeset.Current;
                Console.WriteLine($"Cache updated: {changeset.ReadModelKey}");
            },
            error => Console.WriteLine($"Cache sync error: {error}")
        );
    }

    public Account? GetFromCache(Guid accountId)
    {
        _cache.TryGetValue(accountId, out var account);
        return account;
    }

    public void StopCacheSync()
    {
        _subscription?.Dispose();
    }
}
```

### Background Job Triggering

Automatically trigger processing when read models change:

```csharp
public class BackgroundJobService
{
    readonly IEventStore _eventStore;
    readonly IJobQueue _jobQueue;

    public void WatchAndProcess()
    {
        var observable = _eventStore.ReadModels.Watch<Order>();

        observable.Subscribe(
            async changeset =>
            {
                // Only process completed orders
                if (changeset.Current.Status == OrderStatus.Completed)
                {
                    await _jobQueue.Enqueue(new GenerateInvoiceJob
                    {
                        OrderId = changeset.ReadModelKey,
                        Order = changeset.Current
                    });
                }
            }
        );
    }
}
```

### State Transition Notifications

React to specific state changes:

```csharp
public class StateChangeNotifier
{
    readonly IEventStore _eventStore;

    public void WatchForStateChanges()
    {
        var observable = _eventStore.ReadModels.Watch<Order>();

        observable.Subscribe(changeset =>
        {
            var previous = changeset.Previous;
            var current = changeset.Current;

            // Order shipped
            if (previous?.Status != OrderStatus.Shipped && current.Status == OrderStatus.Shipped)
            {
                NotifyCustomerOfShipment(changeset.ReadModelKey, current);
            }

            // Payment received
            if (previous?.TotalPaid < current.TotalPaid)
            {
                NotifyOfPayment(changeset.ReadModelKey, current.TotalPaid - (previous?.TotalPaid ?? 0m));
            }

            // Order completed
            if (previous?.Status != OrderStatus.Completed && current.Status == OrderStatus.Completed)
            {
                SendFeedbackRequest(changeset.ReadModelKey);
            }
        });
    }
}
```

### Logging and Audit Trail

Log all read model changes for auditing:

```csharp
public class AuditLogger
{
    readonly IEventStore _eventStore;
    readonly ILogger<AuditLogger> _logger;

    public void LogAllChanges()
    {
        var observable = _eventStore.ReadModels.Watch<Order>();

        observable.Subscribe(changeset =>
        {
            _logger.LogInformation(
                "Order changed: Key={OrderKey}, Previous={Previous}, Current={Current}, CorrelationId={CorrelationId}",
                changeset.ReadModelKey,
                changeset.Previous,
                changeset.Current,
                changeset.CorrelationId
            );
        });
    }
}
```

## Filtering and Transforming

### Using LINQ Operators

LINQ operators work on observables for advanced filtering:

```csharp
public class FilteredWatcher
{
    readonly IEventStore _eventStore;

    public void WatchHighValueOrders()
    {
        var observable = _eventStore.ReadModels.Watch<Order>()
            .Where(changeset => changeset.Current.TotalAmount > 1000m)
            .Select(changeset => new
            {
                OrderId = changeset.ReadModelKey,
                Amount = changeset.Current.TotalAmount,
                Status = changeset.Current.Status
            });

        observable.Subscribe(order =>
        {
            Console.WriteLine($"High-value order: {order.OrderId} with amount {order.Amount}");
        });
    }
}
```

### Debouncing Rapid Changes

Handle multiple rapid changes as a single operation:

```csharp
public class TrottledWatcher
{
    readonly IEventStore _eventStore;

    public void WatchWithDebounce()
    {
        var observable = _eventStore.ReadModels.Watch<ShoppingCart>()
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(changeset =>
            {
                // Update UI only once per 500ms, combining rapid changes
                UpdateUI(changeset);
            });
    }
}
```

## Error Handling

### Recovering from Errors

Handle errors gracefully and optionally retry:

```csharp
public class ResilientWatcher
{
    readonly IEventStore _eventStore;

    public void WatchWithErrorHandling()
    {
        var observable = _eventStore.ReadModels.Watch<Account>()
            .Retry(3)  // Automatically retry 3 times on error
            .Catch<ReadModelChangeset<Account>, Exception>(
                error =>
                {
                    _logger.LogError(error, "Watch error, falling back to polling");
                    return StartPollingFallback();
                }
            );

        observable.Subscribe(
            changeset => ProcessChangeset(changeset),
            error => _logger.LogError(error, "Watch failed after retries")
        );
    }
}
```

## Performance Considerations

### Memory Usage

Each subscription holds a reference to keep the observable alive. Always dispose subscriptions when done:

```csharp
public class ManagedWatcher : IDisposable
{
    IDisposable? _subscription;

    public void Start()
    {
        _subscription = _eventStore.ReadModels.Watch<Order>()
            .Subscribe(ProcessChangeset);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

### Selective Watching

Only watch the read models you need to avoid unnecessary processing:

```csharp
// Good - watch only what you need
var orderWatch = _eventStore.ReadModels.Watch<Order>();

// Avoid - watching everything
var allWatches = new[]
{
    _eventStore.ReadModels.Watch<Order>(),
    _eventStore.ReadModels.Watch<Customer>(),
    _eventStore.ReadModels.Watch<Inventory>(),
    _eventStore.ReadModels.Watch<Shipment>()
};
```

## Use Cases

### Real-Time Dashboards

Update dashboard widgets when data changes:

```csharp
public class DashboardHub : Hub
{
    readonly DashboardWatcher _watcher;

    public async Task SubscribeToDashboard(Guid userId)
    {
        _watcher.WatchAccountSummary(userId).Subscribe(
            async changeset =>
            {
                await Clients.User(userId.ToString())
                    .SendAsync("DashboardUpdated", changeset.Current);
            }
        );
    }
}
```

### Event-Driven Architecture

Coordinate actions across multiple systems:

```csharp
public class OrderOrchestrator
{
    public void SetupWorkflow()
    {
        // When an order changes status, trigger appropriate actions
        _eventStore.ReadModels.Watch<Order>()
            .Subscribe(changeset =>
            {
                switch (changeset.Current.Status)
                {
                    case OrderStatus.New:
                        _inventoryService.ReserveItems(changeset.Current);
                        break;
                    case OrderStatus.Confirmed:
                        _paymentService.ProcessPayment(changeset.Current);
                        break;
                    case OrderStatus.Shipped:
                        _notificationService.SendShipmentNotice(changeset.Current);
                        break;
                }
            });
    }
}
```

### Live Collaboration Features

Sync state across multiple users in real-time:

```csharp
public class CollaborativeWorkspace
{
    public void SyncChanges()
    {
        _eventStore.ReadModels.Watch<Document>()
            .Subscribe(changeset =>
            {
                // Broadcast to all users editing this document
                BroadcastToUsers(changeset.Current.DocumentId, changeset.Current);
            });
    }
}
```

## Best Practices

### Always Dispose Subscriptions

Memory leaks can occur if subscriptions aren't disposed:

```csharp
// Good
using var subscription = _eventStore.ReadModels.Watch<Order>().Subscribe(/*...*/);

// Also good
IDisposable? _subscription;

public void Start() => _subscription = _eventStore.ReadModels.Watch<Order>().Subscribe(/*...*/);
public void Stop() => _subscription?.Dispose();
```

### Use Appropriate Threading Models

Be aware of which thread your observer runs on:

```csharp
public void WatchWithUI()
{
    _eventStore.ReadModels.Watch<Order>()
        .ObserveOn(SynchronizationContext.Current)  // Switch to UI thread
        .Subscribe(changeset => UpdateUI(changeset));
}
```

### Handle Backpressure

If processing is slow, manage incoming changes:

```csharp
public void WatchWithBackpressure()
{
    _eventStore.ReadModels.Watch<Order>()
        .Buffer(TimeSpan.FromSeconds(1))  // Batch changes
        .SelectMany(batch => batch)        // Process batch
        .Subscribe(ProcessSlowly);
}
```

## Related Topics

- [Getting a Single Instance](getting-single-instance.md) - Query current read model state
- [Getting a Collection of Instances](getting-collection-instances.md) - Query all instances
- [Getting Snapshots](getting-snapshots.md) - Retrieve historical state snapshots
- [Projections](../projections/index.md) - Learn more about read model projections
- [Reactors](../reactors/index.md) - Event processing with reactors
