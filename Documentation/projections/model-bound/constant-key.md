# Constant Keys

A constant key fixes the read model key to a literal string value, so all events of a given type accumulate into a **single** read model instance regardless of which event source they come from.

## FromEvent with ConstantKey

Use `ConstantKey` on the `[FromEvent]` attribute at the class level to route all matching events to a fixed read model instance:

```csharp
using Cratis.Chronicle.Projections.ModelBound;

[FromEvent<OrderPlaced>(ConstantKey = "global")]
public record GlobalOrderSummary(
    string LastCustomer,
    DateTimeOffset LastOrderDate);
```

Every `OrderPlaced` event from every event source updates the same `GlobalOrderSummary` instance.

## Count, Increment, and Decrement with ConstantKey

`Count`, `Increment`, and `Decrement` attributes also support `ConstantKey` for collecting events from all event sources into a single document:

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record SystemMetrics(
    [Count<OrderPlaced>(ConstantKey = "metrics")]
    int TotalOrders,

    [Increment<UserLoggedIn>(ConstantKey = "metrics")]
    [Decrement<UserLoggedOut>(ConstantKey = "metrics")]
    int ActiveSessions,

    [Count<ErrorOccurred>(ConstantKey = "metrics")]
    int TotalErrors);
```

All three properties converge on the `"metrics"` document regardless of which user or order they come from.

## Mixing event source key and constant key

You can mix regular key-based events with constant key events on the same read model:

```csharp
[FromEvent<UserRegistered>]
public record UserDashboard(
    [Key]
    Guid UserId,

    string Name,

    [Count<OrderPlaced>(ConstantKey = "global-stats")]
    int PlatformTotalOrders);
```

> **Note:** When `ConstantKey` is set on a counter attribute, it affects *which read model instance the counter updates* — not the key of the read model the attribute belongs to. In this example, `PlatformTotalOrders` would update the document with key `"global-stats"`, not the `UserDashboard` instance.

## Complete example

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

// Events
[EventType]
public record ProductPurchased(string ProductId, decimal Amount);

[EventType]
public record ProductReturned(string ProductId, decimal Amount);

[EventType]
public record PageViewed(string PageUrl);

// Global read model
public record StoreMetrics(
    [Count<ProductPurchased>(ConstantKey = "store")]
    int TotalPurchases,

    [Count<ProductReturned>(ConstantKey = "store")]
    int TotalReturns,

    [Increment<ProductPurchased>(ConstantKey = "store")]
    [Decrement<ProductReturned>(ConstantKey = "store")]
    int NetTransactions,

    [Count<PageViewed>(ConstantKey = "store")]
    int TotalPageViews);
```

All events from all users and products accumulate into the single `StoreMetrics` document with key `"store"`.
