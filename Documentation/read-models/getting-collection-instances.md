# Getting a Collection of Instances

The `IReadModels` API provides the `GetInstances<TReadModel>()` method to retrieve all instances of a read model by replaying all events from the event log. This is useful for querying, reporting, and analysis across your entire read model dataset.

## Overview

When you request all instances of a read model using `GetInstances<T>`, Chronicle:

1. Identifies the read model type and its backing projection or reducer
2. Retrieves all events from the event log
3. Applies the projection or reducer logic to build all instances
4. Returns a collection of the resulting read model instances

This method scans the complete event history and produces a snapshot of all read model instances as they currently exist.

## Basic Usage

### Getting All Instances

Retrieve all instances of a read model type:

```csharp
public class ReportingService
{
    readonly IEventStore _eventStore;

    public ReportingService(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<IEnumerable<Account>> GetAllAccounts()
    {
        return await _eventStore.ReadModels.GetInstances<Account>();
    }
}
```

### Filtering Results

The `GetInstances` method returns all instances, which you can then filter using LINQ:

```csharp
public async Task<IEnumerable<Account>> GetHighValueAccounts(decimal threshold)
{
    var allAccounts = await _eventStore.ReadModels.GetInstances<Account>();

    return allAccounts
        .Where(a => a.Balance > threshold)
        .OrderByDescending(a => a.Balance)
        .ToList();
}
```

### Limiting Event Processing

For performance optimization, you can limit the number of events processed:

```csharp
public async Task<IEnumerable<Order>> GetOrdersLimitedToLastEvents()
{
    // Only process the last 1000 events
    return await _eventStore.ReadModels.GetInstances<Order>(EventCount.Create(1000));
}
```

## Working with Different Read Model Types

### Projection-Based Read Models

For read models defined using projections:

```csharp
// Model-bound projection
public record OrderSummary(
    [Key] Guid OrderId,
    [SetFrom<OrderCreated>(nameof(OrderCreated.CustomerId))] Guid CustomerId,
    [SetFrom<OrderCreated>(nameof(OrderCreated.TotalAmount))]
    [Add<PaymentReceived>(nameof(PaymentReceived.Amount))] decimal TotalPaid);

// Retrieve all order summaries
public async Task<IEnumerable<OrderSummary>> GetAllOrderSummaries()
{
    return await _eventStore.ReadModels.GetInstances<OrderSummary>();
}
```

### Reducer-Based Read Models

For read models defined using reducers:

```csharp
public record ShoppingCart(Guid Id, List<CartItem> Items, decimal Total);

public class ShoppingCartReducer : IReducerFor<ShoppingCart>
{
    public ReducerId Identifier => "ShoppingCart";

    public ShoppingCart Initial => new(Guid.Empty, [], 0m);

    public ShoppingCart Reduce(ShoppingCart current, object @event) => @event switch
    {
        CartCreated e => current with { Id = e.CartId },
        ItemAdded e => current with
        {
            Items = [..current.Items, new CartItem(e.ProductId, e.Quantity, e.Price)],
            Total = current.Total + (e.Quantity * e.Price)
        },
        _ => current
    };
}

// Retrieve all shopping carts
public async Task<IEnumerable<ShoppingCart>> GetAllActiveCarts()
{
    var carts = await _eventStore.ReadModels.GetInstances<ShoppingCart>();
    return carts.Where(c => c.Items.Any()).ToList();
}
```

## Common Queries

### Getting Statistics

Compute aggregates across all instances:

```csharp
public async Task PrintAccountStatistics()
{
    var accounts = await _eventStore.ReadModels.GetInstances<Account>();

    var stats = new
    {
        TotalAccounts = accounts.Count(),
        AverageBalance = accounts.Average(a => a.Balance),
        TotalBalance = accounts.Sum(a => a.Balance),
        HighestBalance = accounts.Max(a => a.Balance),
        LowestBalance = accounts.Min(a => a.Balance)
    };

    Console.WriteLine($"Total Accounts: {stats.TotalAccounts}");
    Console.WriteLine($"Average Balance: {stats.AverageBalance:C}");
    Console.WriteLine($"Total Balance: {stats.TotalBalance:C}");
}
```

### Finding Specific Instances

Search across all instances for specific conditions:

```csharp
public async Task<User?> FindUserByEmail(string email)
{
    var allUsers = await _eventStore.ReadModels.GetInstances<User>();
    return allUsers.FirstOrDefault(u => u.Email == email);
}

public async Task<IEnumerable<Order>> FindOrdersByStatus(OrderStatus status)
{
    var allOrders = await _eventStore.ReadModels.GetInstances<Order>();
    return allOrders.Where(o => o.Status == status).ToList();
}
```

### Grouping and Aggregation

Group instances for reporting:

```csharp
public async Task PrintTransactionsByType()
{
    var transactions = await _eventStore.ReadModels.GetInstances<Transaction>();

    var grouped = transactions
        .GroupBy(t => t.Type)
        .Select(g => new
        {
            Type = g.Key,
            Count = g.Count(),
            TotalAmount = g.Sum(t => t.Amount)
        });

    foreach (var group in grouped)
    {
        Console.WriteLine($"{group.Type}: {group.Count} transactions, Total: {group.TotalAmount:C}");
    }
}
```

## Performance Considerations

### Event History Impact

The performance of `GetInstances` depends on the total number of events in the event log:

- **Small event logs** (hundreds of events): Fast, typically under 500ms
- **Medium event logs** (thousands of events): Moderate, typically under 2 seconds
- **Large event logs** (tens of thousands of events): Slower, may take several seconds

### Memory Usage

All instances are loaded into memory. For large datasets:

- **Large number of instances**: Consider pagination or filtering at the query level
- **Large individual instances**: Be aware of total memory consumption
- **Streaming alternative**: For very large datasets, consider using materialized projections stored in a database instead

### When to Use GetInstances

Use `GetInstances` when:

- You need to report on all instances
- You're performing analysis across your entire dataset
- The dataset is reasonably small (hundreds to low thousands of instances)

Avoid `GetInstances` when:

- You only need a single instance (use `GetInstanceById` instead)
- You have a very large number of read model instances
- You need filtering/pagination capabilities (use materialized projections instead)

## Caching Strategies

For reports that are generated infrequently, you can cache the entire result set:

```csharp
public class CachedReportingService
{
    readonly IEventStore _eventStore;
    readonly IMemoryCache _cache;

    public async Task<IEnumerable<Account>> GetAllAccountsCached()
    {
        const string cacheKey = "all-accounts";

        if (_cache.TryGetValue<IEnumerable<Account>>(cacheKey, out var cached))
        {
            return cached;
        }

        var accounts = await _eventStore.ReadModels.GetInstances<Account>();

        // Cache for 1 hour
        _cache.Set(cacheKey, accounts, TimeSpan.FromHours(1));

        return accounts;
    }
}
```

**Important**: Always implement cache invalidation when events are appended that affect your read models.

## Use Cases

### Reporting and Analytics

Generate reports across all instances:

```csharp
public async Task ExportAccountsReport()
{
    var accounts = await _eventStore.ReadModels.GetInstances<Account>();

    var report = accounts
        .OrderByDescending(a => a.Balance)
        .Select(a => new { a.Id, a.Name, a.Balance, a.CreatedDate });

    await ExportToCsv(report);
}
```

### Bulk Operations

Process all instances for administrative tasks:

```csharp
public async Task RecalculateAllProjections()
{
    var orders = await _eventStore.ReadModels.GetInstances<Order>();

    foreach (var order in orders)
    {
        await ProcessOrderForAnalytics(order);
    }
}

### Time Travel and Point-in-Time Reads

Rebuild a read model collection up to a known point in time by pairing the event count limit with a captured sequence position from the event sequence state. This is useful for audits, back-testing, and historical reporting. See [Getting state](../events/getting-state.md) for how to capture the sequence position.
```

### Data Validation

Verify data integrity across all instances:

```csharp
public async Task ValidateDataIntegrity()
{
    var carts = await _eventStore.ReadModels.GetInstances<ShoppingCart>();

    var invalidCarts = carts.Where(c => c.Items.Sum(i => i.Quantity) < 0).ToList();

    if (invalidCarts.Any())
    {
        foreach (var cart in invalidCarts)
        {
            Console.WriteLine($"Invalid cart found: {cart.Id}");
        }
    }
}
```

## Related Topics

- [Getting a Single Instance](getting-single-instance.md) - Retrieve a specific read model instance by key
- [Getting Snapshots](getting-snapshots.md) - Retrieve historical state snapshots
- [Watching Read Models](watching-read-models.md) - Real-time notifications for read model changes
- [Projections](../projections/index.md) - Learn more about defining projections
- [Reducers](../reducers/index.md) - Learn more about defining reducers
- [Events - Getting state](../events/getting-state.md) - Capture sequence position for point-in-time reads
