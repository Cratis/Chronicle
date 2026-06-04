# Watching Read Models

The `IReadModels` API provides the `Watch<TReadModel>()` method to observe real-time changes to read models. This is useful for building reactive user interfaces, triggering background jobs, or maintaining synchronized caches based on read model updates.

> [!TIP]
> For information on how to define and configure read models in Arc, see <xref:Arc.Chronicle.ReadModels>.

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
                Console.WriteLine($"Order {changeset.ModelKey} changed");
                Console.WriteLine($"New state: {changeset.ReadModel}");
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
    EventStoreNamespaceName Namespace,      // The namespace for the event store
    ReadModelKey ModelKey,                  // The key of the changed instance
    TReadModel? ReadModel,                  // The current state of the instance (null when removed)
    bool Removed);                          // Whether the read model was removed
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
                    orderId = changeset.ModelKey,
                    order = changeset.ReadModel,
                    removed = changeset.Removed
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
                if (changeset.Removed)
                {
                    _cache.Remove(changeset.ModelKey);
                }
                else if (changeset.ReadModel is not null)
                {
                    _cache[changeset.ModelKey] = changeset.ReadModel;
                }

                Console.WriteLine($"Cache updated: {changeset.ModelKey}");
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
                if (changeset.ReadModel?.Status == OrderStatus.Completed)
                {
                    await _jobQueue.Enqueue(new GenerateInvoiceJob
                    {
                        OrderId = changeset.ModelKey,
                        Order = changeset.ReadModel
                    });
                }
            }
        );
    }
}
```

### State Transition Notifications

React to specific states as the read model reaches them:

```csharp
public class StateChangeNotifier
{
    readonly IEventStore _eventStore;

    public void WatchForStateChanges()
    {
        var observable = _eventStore.ReadModels.Watch<Order>();

        observable.Subscribe(changeset =>
        {
            if (changeset.Removed || changeset.ReadModel is null)
            {
                return;
            }

            var current = changeset.ReadModel;

            // Order shipped
            if (current.Status == OrderStatus.Shipped)
            {
                NotifyCustomerOfShipment(changeset.ModelKey, current);
            }

            // Order completed
            if (current.Status == OrderStatus.Completed)
            {
                SendFeedbackRequest(changeset.ModelKey);
            }
        });
    }
}
```

The changeset carries the current state of the read model, not its previous state. If you need to react to a transition (the difference between two states), track the last value you saw per `ModelKey` yourself, or derive the transition from a dedicated event using a [reactor](../reactors/index.md).

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
                "Order changed: Key={OrderKey}, Current={Current}, Removed={Removed}",
                changeset.ModelKey,
                changeset.ReadModel,
                changeset.Removed
            );
        });
    }
}
```

## Filtering and Transforming

### Simplifying with ToObservableReadModel()

When you only need the read model instances themselves and don't care about the changeset metadata (like the key, previous state, or events), use the `.ToObservableReadModel()` extension method to simplify your subscription:

```csharp
public class SimpleWatcher
{
    readonly IEventStore _eventStore;

    public void WatchOrders()
    {
        // ToObservableReadModel() filters out removed items and emits only the read model instances
        var observable = _eventStore.ReadModels.Watch<Order>()
            .ToObservableReadModel();

        observable.Subscribe(
            order =>
            {
                // Receive the Order instance directly, not the full changeset
                Console.WriteLine($"Order status: {order.Status}");
                Console.WriteLine($"Amount: {order.TotalAmount}");
            },
            error => Console.WriteLine($"Error: {error}"),
            () => Console.WriteLine("Watch completed")
        );
    }
}
```

This extension method:
- Automatically filters out removed read models
- Skips null read model instances
- Returns an `ISubject<TReadModel>` instead of `IObservable<ReadModelChangeset<TReadModel>>`
- Propagates errors and completion signals

This is particularly useful when building reactive UIs where you want to bind directly to the read model state without dealing with changeset structures.

### Using LINQ Operators

LINQ operators work on observables for advanced filtering:

```csharp
public class FilteredWatcher
{
    readonly IEventStore _eventStore;

    public void WatchHighValueOrders()
    {
        var observable = _eventStore.ReadModels.Watch<Order>()
            .Where(changeset => changeset.ReadModel?.TotalAmount > 1000m)
            .Select(changeset => new
            {
                OrderId = changeset.ModelKey,
                Amount = changeset.ReadModel!.TotalAmount,
                Status = changeset.ReadModel!.Status
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
                    .SendAsync("DashboardUpdated", changeset.ReadModel);
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
                if (changeset.ReadModel is null)
                {
                    return;
                }

                switch (changeset.ReadModel.Status)
                {
                    case OrderStatus.New:
                        _inventoryService.ReserveItems(changeset.ReadModel);
                        break;
                    case OrderStatus.Confirmed:
                        _paymentService.ProcessPayment(changeset.ReadModel);
                        break;
                    case OrderStatus.Shipped:
                        _notificationService.SendShipmentNotice(changeset.ReadModel);
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
                if (changeset.ReadModel is null)
                {
                    return;
                }

                // Broadcast to all users editing this document
                BroadcastToUsers(changeset.ReadModel.DocumentId, changeset.ReadModel);
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
