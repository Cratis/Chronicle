---
uid: explicit-subscriptions
title: Explicit Event Store Subscriptions
---

# Explicit Event Store Subscriptions

Chronicle supports **explicit subscriptions** — manual registration of subscriptions between event stores using the `Subscriptions` API. When you configure a subscription explicitly, you control:

- Which event types are forwarded
- Subscription lifecycle and management
- The subscription identifier for reference and cleanup

Explicit subscriptions are ideal when:
- You need fine-grained control over which event types are forwarded
- The source service's event types are not in a shared NuGet package
- You want to dynamically create or remove subscriptions
- Events come from a legacy or external system

## Setting Up an Explicit Subscription

Use the `Subscriptions` property on `IEventStore` to subscribe:

```csharp
await eventStore.Subscriptions.Subscribe(
    "orders-from-fulfillment",
    "fulfillment-service",
    builder => builder.WithEventType<ShipmentDispatched>());
```

The three arguments are:

| Argument | Description |
|---|---|
| Subscription ID | A stable, unique string that identifies this subscription within the target event store |
| Source event store | The name of the event store whose outbox to subscribe to |
| Configuration callback | Optional — filters which event types to forward |

### Subscription ID

The subscription ID must be unique within the target event store. It serves as the persistent identifier for the subscription on the kernel side. If you call `Subscribe` with the same ID, the kernel treats it as idempotent — no duplicate subscription is created.

A typical naming convention:

```csharp
// subscription-id format: {target}-from-{source}
await eventStore.Subscriptions.Subscribe(
    "orders-from-fulfillment",
    "fulfillment-service",
    builder => builder.WithEventType<ShipmentDispatched>());

await eventStore.Subscriptions.Subscribe(
    "inventory-from-warehouse",
    "warehouse-service",
    builder => builder.WithEventType<StockAdjusted>());
```

## Filtering Event Types

Without a configuration callback, **all events** from the source outbox are forwarded. Use `WithEventType<T>()` to limit the subscription to specific types:

```csharp
await eventStore.Subscriptions.Subscribe(
    "inventory-updates",
    "warehouse-service",
    builder => builder
        .WithEventType<StockAdjusted>()
        .WithEventType<StockReserved>());
```

If you need to subscribe to all events without filtering, omit the configuration callback entirely:

```csharp
// All events from fulfillment-service outbox will be forwarded
await eventStore.Subscriptions.Subscribe(
    "all-fulfillment-events",
    "fulfillment-service");
```

## Accessing Forwarded Events

Events forwarded via an explicit subscription are placed into the inbox sequence for the source event store. Access inbox events through any observer (reactor, projection, reducer):

```csharp
[EventType]
public record OrderPlaced(Guid OrderId, decimal Amount);

public class IncomingOrdersReactor : IReactor
{
    public Task OrderPlaced(OrderPlaced @event, EventContext context)
        => HandleIncomingOrderAsync(@event.OrderId, @event.Amount);

    Task HandleIncomingOrderAsync(Guid id, decimal amount) => Task.CompletedTask;
}
```

The kernel automatically routes incoming events to the appropriate `inbox-{sourceEventStore}` event sequence. You do not need to reference the subscription ID when observing the events.

## Removing a Subscription

```csharp
await eventStore.Subscriptions.Unsubscribe("orders-from-fulfillment");
```

Calling `Unsubscribe` with the subscription ID:
- Stops event forwarding from the source outbox
- Removes the persistent subscription definition from the kernel
- Existing inbox events are **not deleted** — they remain available for observation

Once removed, no new events are forwarded to the inbox. If you later recreate the subscription with the same ID, forwarding resumes from that point forward.

## Idempotent Registration

Subscriptions are identified by their subscription ID. Calling `Subscribe` with the same ID more than once is safe — if the subscription already exists, no duplicate subscription is created.

This makes it safe to register all subscriptions at application startup without checking if they already exist:

```csharp
// Safe to call on every application startup
var app = builder.Build();

var eventStore = app.Services.GetRequiredService<IEventStore>();

await eventStore.Subscriptions.Subscribe(
    "orders-from-fulfillment",
    "fulfillment-service",
    builder => builder.WithEventType<ShipmentDispatched>());

await eventStore.Subscriptions.Subscribe(
    "inventory-from-warehouse",
    "warehouse-service",
    builder => builder.WithEventType<StockAdjusted>());

await app.RunAsync();
```

If any of these subscriptions already exist on the kernel, they are left unchanged.

## Subscription Lifecycle

Explicit subscriptions are **kernel-managed** and **persistent**:

- **Created** when you call `Subscribe`, stored on the kernel, and assigned a subscription ID
- **Persisted** — survive client disconnections and kernel restarts
- **Re-established** automatically when the kernel starts, even if the client is not connected
- **Removed** only when you explicitly call `Unsubscribe`

## Typical Setup Pattern

A common pattern is to register all subscriptions once during application startup:

```csharp
public static async Task RegisterIntegrationSubscriptions(IEventStore eventStore)
{
    await eventStore.Subscriptions.Subscribe(
        "orders-from-fulfillment",
        "fulfillment-service",
        builder => builder.WithEventType<ShipmentDispatched>());

    await eventStore.Subscriptions.Subscribe(
        "inventory-updates",
        "warehouse-service",
        builder => builder
            .WithEventType<StockAdjusted>()
            .WithEventType<StockReserved>());
}

// In Program.cs
var app = builder.Build();
var eventStore = app.Services.GetRequiredService<IEventStore>();
await RegisterIntegrationSubscriptions(eventStore);
await app.RunAsync();
```

## When to Use Explicit Subscriptions

**Use explicit subscriptions when:**
- You need fine-grained control over which event types are forwarded
- Event types are not published in a shared NuGet package
- You need to dynamically create or remove subscriptions at runtime
- The subscription relationship is consuming-service-specific

**Use [implicit subscriptions](implicit-subscriptions.md) when:**
- Event types are published in a shared NuGet package with `[EventStore]` attributes
- All events from a source should be automatically forwarded
- You prefer zero configuration and automatic routing

## See Also

- [Implicit Event Store Subscriptions](implicit-subscriptions.md) — automatic subscription via `[EventStore]` attributes
- [Outbox and Inbox](outbox-inbox.md) — conceptual explanation of how events flow between stores
- [Reactors](../reactors/index.md) — processing inbox events with reactors
- [Projections](../projections/index.md) — building read models from inbox events
