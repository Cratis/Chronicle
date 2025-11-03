# Counters

Model-bound projections provide three counter operations for tracking occurrences and quantities: Increment, Decrement, and Count.

## Increment

The `Increment` attribute increments a numeric property when an event occurs. This is useful for tracking counters that increase with specific events.

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record UserStatistics(
    [Key, FromEventSourceId]
    Guid UserId,
    
    [Increment<UserLoggedIn>]
    int LoginCount);
```

Each time a `UserLoggedIn` event occurs, `LoginCount` is incremented by 1.

## Decrement

The `Decrement` attribute decrements a numeric property when an event occurs. This is useful for tracking decreasing counters.

```csharp
public record ServerStatistics(
    [Key, FromEventSourceId]
    Guid ServerId,
    
    [Increment<UserConnected>]
    [Decrement<UserDisconnected>]
    int ActiveConnections);
```

When a `UserConnected` event occurs, `ActiveConnections` increases by 1. When a `UserDisconnected` event occurs, it decreases by 1.

## Count

The `Count` attribute counts the total number of times an event occurs. Unlike Increment, Count doesn't increment from a current value—it maintains an absolute count.

```csharp
public record EventMetrics(
    [Key, FromEventSourceId]
    Guid Id,
    
    [Count<OrderPlaced>]
    int TotalOrders,
    
    [Count<OrderCancelled>]
    int CancelledOrders);
```

## Multiple Events

You can use multiple attributes on the same property to respond to different events:

```csharp
public record InventoryItem(
    [Key, FromEventSourceId]
    Guid ItemId,
    
    [SetFrom<ItemCreated>(nameof(ItemCreated.Name))]
    string Name,
    
    [SetFrom<ItemCreated>(nameof(ItemCreated.InitialQuantity))]
    [Increment<ItemRestocked>]
    [Decrement<ItemSold>]
    int Quantity,
    
    [Count<ItemRestocked>]
    int RestockCount,
    
    [Count<ItemSold>]
    int SalesCount);
```

## Complete Example

Here's a complete example tracking various metrics:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

// Events
[EventType]
public record UserLoggedIn(DateTimeOffset Timestamp);

[EventType]
public record UserLoggedOut(DateTimeOffset Timestamp);

[EventType]
public record PurchaseMade(decimal Amount);

[EventType]
public record RefundIssued(decimal Amount);

// Read Model
public record UserActivity(
    [Key, FromEventSourceId]
    Guid UserId,
    
    // Track login/logout counts
    [Count<UserLoggedIn>]
    int TotalLogins,
    
    [Count<UserLoggedOut>]
    int TotalLogouts,
    
    // Track active sessions
    [Increment<UserLoggedIn>]
    [Decrement<UserLoggedOut>]
    int ActiveSessions,
    
    // Track transaction counts
    [Count<PurchaseMade>]
    int PurchaseCount,
    
    [Count<RefundIssued>]
    int RefundCount,
    
    // Track transaction values
    [AddFrom<PurchaseMade>(nameof(PurchaseMade.Amount))]
    [SubtractFrom<RefundIssued>(nameof(RefundIssued.Amount))]
    decimal NetSpent);
```

## Counter vs Count

**Increment/Decrement:**
- Modifies the current value
- Useful for tracking active states (sessions, connections)
- Can be combined with SetFrom to establish initial values
- Changes are relative to current value

**Count:**
- Maintains absolute count of event occurrences
- Useful for analytics and reporting
- Independent of other operations
- Always represents total occurrences

## Best Practices

1. **Use Increment/Decrement** for tracking active/current states that change over time
2. **Use Count** for analytics and metrics that track total occurrences
3. **Combine operations** on the same property when tracking both current state and history
4. **Initialize counters** with SetFrom when you have an initial value from a creation event
