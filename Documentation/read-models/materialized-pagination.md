# Materialized Read Models - Pagination and Observation

The `IMaterializedReadModels` interface provides paginated access and real-time observation of materialized read model instances stored in sinks (MongoDB, SQL, etc.). This is useful when you need to work with large datasets efficiently or observe changes to stored read models.

## Overview

Materialized read models are already computed and stored in a database sink, making them ideal for:

- **Paginated queries** - Retrieve manageable subsets of large datasets
- **Real-time monitoring** - Observe changes to stored instances via change streams
- **Performance** - Skip expensive event replay when data is already materialized
- **Large datasets** - Handle thousands or millions of instances efficiently

Access materialized read models through the `Materialized` property on `IReadModels`:

```csharp
var instances = await eventStore.ReadModels.Materialized.GetInstances<MyModel>(
    skip: 10,
    take: 20);
```

## Getting Paginated Instances

### Basic Pagination

Retrieve a specific page of instances:

```csharp
public class ProductListService
{
    readonly IEventStore _eventStore;

    public ProductListService(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<IEnumerable<Product>> GetProductPage(int pageNumber, int pageSize)
    {
        var skip = pageNumber * pageSize;
        var products = await _eventStore.ReadModels.Materialized.GetInstances<Product>(
            skip: skip,
            take: pageSize);

        return products;
    }
}
```

### Type-Safe Parameters

The pagination parameters use strongly-typed concepts with implicit conversion:

```csharp
// Implicit conversion from int (recommended)
var instances = await readModels.Materialized.GetInstances<Order>(
    skip: 0,
    take: 50);

// Explicit concept types (also supported)
var instances = await readModels.Materialized.GetInstances<Order>(
    skip: InstanceCountToSkip.Zero,
    take: InstanceCount.Default);

// Using named constants
var instances = await readModels.Materialized.GetInstances<Order>(
    skip: InstanceCountToSkip.Zero,      // Predefined: skip nothing
    take: InstanceCount.Default);        // Predefined: 50 items
```

### Building a Paginated API

```csharp
public class OrdersController : ControllerBase
{
    readonly IEventStore _eventStore;

    [HttpGet]
    public async Task<ActionResult<PagedResult<Order>>> GetOrders(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        var skip = page * pageSize;
        var orders = await _eventStore.ReadModels.Materialized.GetInstances<Order>(
            skip: skip,
            take: pageSize);

        return Ok(new PagedResult<Order>
        {
            Items = orders,
            Page = page,
            PageSize = pageSize
        });
    }
}
```

## Observing Instances

### Real-Time Monitoring

Watch for changes to materialized instances:

```csharp
public class InventoryMonitor
{
    readonly IEventStore _eventStore;
    IDisposable? _subscription;

    public void StartMonitoring()
    {
        var observable = _eventStore.ReadModels.Materialized.ObserveInstances<Product>(
            skip: 0,
            take: 100);

        _subscription = observable.Subscribe(
            products =>
            {
                Console.WriteLine($"Received {products.Count()} products");
                foreach (var product in products)
                {
                    Console.WriteLine($"  {product.Name}: {product.StockLevel} units");
                }
            },
            error => Console.WriteLine($"Error: {error}"),
            () => Console.WriteLine("Observation completed"));
    }

    public void StopMonitoring()
    {
        _subscription?.Dispose();
    }
}
```

### Paginated Observation

Observe a specific range of instances:

```csharp
public class TopOrdersWatcher
{
    public void WatchTopOrders()
    {
        // Watch only the first 50 orders
        var observable = _eventStore.ReadModels.Materialized.ObserveInstances<Order>(
            skip: 0,
            take: 50);

        observable.Subscribe(orders =>
        {
            var topOrder = orders.OrderByDescending(o => o.TotalAmount).FirstOrDefault();
            if (topOrder != null)
            {
                Console.WriteLine($"Highest order: ${topOrder.TotalAmount}");
            }
        });
    }
}
```

### Live Dashboard Updates

Push updates to connected clients:

```csharp
public class DashboardHub : Hub
{
    readonly IEventStore _eventStore;
    readonly ConcurrentDictionary<string, IDisposable> _subscriptions = new();

    public async Task SubscribeToProducts(int page, int pageSize)
    {
        var connectionId = Context.ConnectionId;

        var skip = page * pageSize;
        var observable = _eventStore.ReadModels.Materialized.ObserveInstances<Product>(
            skip: skip,
            take: pageSize);

        var subscription = observable.Subscribe(
            async products =>
            {
                await Clients.Client(connectionId).SendAsync("ProductsUpdated", new
                {
                    page,
                    pageSize,
                    items = products
                });
            });

        _subscriptions[connectionId] = subscription;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (_subscriptions.TryRemove(Context.ConnectionId, out var subscription))
        {
            subscription.Dispose();
        }
        return base.OnDisconnectedAsync(exception);
    }
}
```

## Performance Considerations

### When to Use Materialized Access

Use `Materialized.GetInstances()` and `ObserveInstances()` when:

- Working with projection-based or reducer-based read models stored in MongoDB or SQL
- Dataset contains hundreds to millions of instances
- Need efficient pagination without loading everything into memory
- Want to observe changes to stored data via change streams

### Memory Efficiency

Paginated access loads only the requested page:

```csharp
// Efficient - only loads 20 instances
var page1 = await readModels.Materialized.GetInstances<Order>(skip: 0, take: 20);
var page2 = await readModels.Materialized.GetInstances<Order>(skip: 20, take: 20);

// Less efficient for large datasets - loads everything
var allOrders = await readModels.GetInstances<Order>();
```

### Change Stream Support

Observation uses database change streams when available:

- **MongoDB**: Uses native MongoDB change streams for real-time updates
- **SQL Server**: Uses `DbContext.Observe()` with polling or EF triggers
- **In-Memory/Testing**: Simulated observation for testing scenarios

## Use Cases

### Paginated Data Grids

Build efficient data tables with server-side pagination:

```csharp
public class OrderGridService
{
    public async Task<GridResult> LoadOrderGrid(GridRequest request)
    {
        var orders = await _eventStore.ReadModels.Materialized.GetInstances<Order>(
            skip: request.Skip,
            take: request.PageSize);

        return new GridResult
        {
            Data = orders,
            Skip = request.Skip,
            PageSize = request.PageSize
        };
    }
}
```

### Infinite Scrolling

Implement progressive loading for infinite scroll UIs:

```csharp
public class InfiniteScrollService
{
    int _currentPage = 0;
    const int PageSize = 20;

    public async Task<IEnumerable<Product>> LoadNextPage()
    {
        var products = await _eventStore.ReadModels.Materialized.GetInstances<Product>(
            skip: _currentPage * PageSize,
            take: PageSize);

        _currentPage++;
        return products;
    }
}
```

### Real-Time Reporting

Monitor top performers in real-time:

```csharp
public class SalesMonitor
{
    public void MonitorTopSellers()
    {
        _eventStore.ReadModels.Materialized.ObserveInstances<ProductSales>(
            skip: 0,
            take: 10)
            .Subscribe(topSellers =>
            {
                Console.WriteLine("Top 10 Products:");
                foreach (var product in topSellers.OrderByDescending(p => p.TotalSales))
                {
                    Console.WriteLine($"  {product.Name}: ${product.TotalSales}");
                }
            });
    }
}
```

## Comparison with GetInstances

| Feature | `ReadModels.GetInstances<T>()` | `ReadModels.Materialized.GetInstances<T>(skip, take)` |
|---------|--------------------------------|-------------------------------------------------------|
| **Data Source** | Event log replay | Materialized sink (DB) |
| **Performance** | Slower (event replay) | Faster (direct DB query) |
| **Pagination** | No | Yes (skip/take) |
| **Dataset Size** | Small to medium | Small to very large |
| **Memory Usage** | Loads all instances | Loads only requested page |
| **Currency** | Always up-to-date | Eventually consistent |

## Best Practices

### Always Dispose Subscriptions

Prevent memory leaks by disposing observation subscriptions:

```csharp
public class ManagedObserver : IDisposable
{
    IDisposable? _subscription;

    public void Start()
    {
        _subscription = _eventStore.ReadModels.Materialized
            .ObserveInstances<Product>(0, 100)
            .Subscribe(HandleUpdate);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

### Use Appropriate Page Sizes

Balance between network overhead and responsiveness:

```csharp
// Good for UI pagination
const int UiPageSize = 20;

// Good for background processing
const int BatchPageSize = 100;

// Too small - excessive round trips
const int TooSmall = 5;

// Too large - high memory and latency
const int TooLarge = 10000;
```

### Handle Errors Gracefully

```csharp
public void ObserveWithErrorHandling()
{
    _eventStore.ReadModels.Materialized.ObserveInstances<Order>(0, 50)
        .Retry(3)
        .Subscribe(
            orders => ProcessOrders(orders),
            error => _logger.LogError(error, "Observation failed"),
            () => _logger.LogInformation("Observation completed"));
}
```

## Related Topics

- [Getting a Collection of Instances](getting-collection-instances.md) - Retrieve all instances via event replay
- [Watching Read Models](watching-read-models.md) - Observe read model changesets
- [Projections](../projections/index.md) - Define materialized read models
- [Reducers](../reducers/index.md) - Define state-based read models
- [MongoDB Storage](../storage/mongodb.md) - MongoDB sink implementation
- [SQL Storage](../storage/sql.md) - SQL Server sink implementation
