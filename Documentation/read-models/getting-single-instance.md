# Getting a Single Instance

The `IReadModels` API provides a straightforward way to retrieve the current state of a read model by replaying events from the event log. This ensures you always get strongly consistent data reflecting the exact current state.

## Overview

When you request a read model instance using `GetInstanceById`, Chronicle:

1. Identifies whether the read model is produced by a projection or reducer
2. Retrieves all relevant events from the event log for the specified key
3. Applies the projection or reducer logic to these events in memory
4. Returns the resulting read model instance

This on-demand computation ensures strong consistency - you get the most up-to-date state without waiting for eventual consistency.

## Basic Usage

### Using Generic Method

The generic method provides type safety:

```csharp
public class AccountService
{
    readonly IEventStore _eventStore;

    public AccountService(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<AccountInfo?> GetAccountInfo(Guid accountId)
    {
        return await _eventStore.ReadModels.GetInstanceById<AccountInfo>(accountId);
    }
}
```

### Using Non-Generic Method

For scenarios where the type is determined at runtime:

```csharp
public async Task<object> GetReadModelInstance(Type readModelType, ReadModelKey key)
{
    return await _eventStore.ReadModels.GetInstanceById(readModelType, key);
}
```

## Working with Different Read Model Types

### Projection-Based Read Models

For read models defined using projections (either model-bound or declarative):

```csharp
// Model-bound projection
public record OrderSummary(
    [Key] Guid OrderId,
    [SetFrom<OrderCreated>(nameof(OrderCreated.CustomerId))] Guid CustomerId,
    [SetFrom<OrderCreated>(nameof(OrderCreated.TotalAmount))]
    [Add<PaymentReceived>(nameof(PaymentReceived.Amount))] decimal TotalPaid);

// Retrieve instance
public async Task<OrderSummary?> GetOrderSummary(Guid orderId)
{
    return await _eventStore.ReadModels.GetInstanceById<OrderSummary>(orderId);
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

// Retrieve instance
public async Task<ShoppingCart?> GetCart(Guid cartId)
{
    return await _eventStore.ReadModels.GetInstanceById<ShoppingCart>(cartId);
}
```

## Handling Null Results

Read models that haven't received any events will return a default or initial state:

```csharp
public async Task<AccountInfo> GetOrCreateDefaultAccount(Guid accountId)
{
    var account = await _eventStore.ReadModels.GetInstanceById<AccountInfo>(accountId);

    // For new accounts with no events, you'll get default values
    if (account.Name == string.Empty)
    {
        // This is a new account that hasn't been initialized
        return new AccountInfo(accountId, "New Account", 0m);
    }

    return account;
}
```

## Performance Considerations

### Event History Length

The performance of `GetInstanceById` depends on the number of events that need to be replayed:

- **Short histories** (dozens of events): Fast, typically under 100ms
- **Medium histories** (hundreds of events): Moderate, typically under 500ms
- **Long histories** (thousands of events): Slower, may take seconds

For read models with very long event histories that are accessed frequently, consider using materialized projections with database storage instead of on-demand computation.

### Caching Strategies

For read models accessed frequently within a short time window:

```csharp
public class CachedAccountService
{
    readonly IEventStore _eventStore;
    readonly IMemoryCache _cache;

    public async Task<AccountInfo?> GetAccountInfo(Guid accountId)
    {
        var cacheKey = $"account:{accountId}";

        if (_cache.TryGetValue<AccountInfo>(cacheKey, out var cached))
        {
            return cached;
        }

        var account = await _eventStore.ReadModels.GetInstanceById<AccountInfo>(accountId);

        _cache.Set(cacheKey, account, TimeSpan.FromMinutes(5));

        return account;
    }
}
```

**Important**: Only cache read models when you can accept the risk of serving slightly stale data. Always invalidate the cache when new events are appended that affect the read model.

## Use Cases

### Real-Time Dashboards

When you need the absolute latest state:

```csharp
public async Task<DashboardData> GetCurrentDashboard(Guid userId)
{
    var profile = await _eventStore.ReadModels.GetInstanceById<UserProfile>(userId);
    var stats = await _eventStore.ReadModels.GetInstanceById<UserStatistics>(userId);

    return new DashboardData(profile, stats);
}
```

### Financial Transactions

When accuracy is critical and eventual consistency is unacceptable:

```csharp
public async Task<bool> CanWithdraw(Guid accountId, decimal amount)
{
    var account = await _eventStore.ReadModels.GetInstanceById<Account>(accountId);
    return account.Balance >= amount;
}
```

### Command Validation

When you need to validate commands against current state:

```csharp
public async Task<Result> ProcessOrder(PlaceOrderCommand command)
{
    var inventory = await _eventStore.ReadModels.GetInstanceById<ProductInventory>(command.ProductId);

    if (inventory.AvailableQuantity < command.Quantity)
    {
        return Result.Failure("Insufficient inventory");
    }

    // Process the order
    await _eventStore.EventLog.Append(command.OrderId, new OrderPlaced(/* ... */));

    return Result.Success();
}
```

### Read-After-Write Consistency

When you append events and immediately need the updated state:

```csharp
public async Task<Account> DepositMoney(Guid accountId, decimal amount)
{
    // Append the event
    await _eventStore.EventLog.Append(accountId, new MoneyDeposited(amount));

    // Get the updated state immediately
    return await _eventStore.ReadModels.GetInstanceById<Account>(accountId);
}
```

## Best Practices

### Choose the Right Approach

- **On-demand retrieval** (`GetInstanceById`): Use when strong consistency is required and the read model isn't accessed frequently
- **Materialized projections**: Use for frequently accessed read models or those with long event histories
- **Projection watchers**: Use for real-time updates in user interfaces

### Consider Access Patterns

- **Infrequent access + need for accuracy**: Perfect for `GetInstanceById`
- **Frequent access + long event history**: Consider materialized projections
- **Frequent access + short event history**: `GetInstanceById` with caching may be appropriate

### Type Safety

Always use the generic method when the type is known at compile time:

```csharp
// Good - type safe
var account = await _eventStore.ReadModels.GetInstanceById<Account>(accountId);

// Avoid unless type is truly unknown at compile time
var account = (Account)await _eventStore.ReadModels.GetInstanceById(typeof(Account), accountId);
```

## Related Topics

- [Getting a Collection of Instances](getting-collection-instances.md) - Learn how to retrieve all instances of a read model
- [Getting Snapshots](getting-snapshots.md) - Learn how to retrieve historical state snapshots
- [Watching Read Models](watching-read-models.md) - Real-time notifications for read model changes
- [Projections](../projections/index.md) - Learn more about defining projections
- [Reducers](../recipes/reducers.md) - Learn more about defining reducers
