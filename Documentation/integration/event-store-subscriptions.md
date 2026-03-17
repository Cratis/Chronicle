# Event Store Subscriptions

Event store subscriptions let one event store receive events from another's outbox. The subscription is registered once, persisted on the Kernel, and active for as long as the subscription exists — regardless of whether the originating client is connected.

## Setting Up a Subscription

Use the `Subscriptions` property on `IEventStore` to subscribe declaratively:

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

## Filtering Event Types

Without a configuration callback, all events from the source outbox are forwarded. Use `WithEventType<T>()` to limit the subscription to specific types:

```csharp
await eventStore.Subscriptions.Subscribe(
    "inventory-updates",
    "warehouse-service",
    builder => builder
        .WithEventType<StockAdjusted>()
        .WithEventType<StockReserved>());
```

## Removing a Subscription

```csharp
await eventStore.Subscriptions.Unsubscribe("orders-from-fulfillment");
```

Removing a subscription stops event forwarding and removes the persisted subscription definition from the Kernel.

## Idempotent Registration

Subscriptions are identified by their subscription ID. Calling `Subscribe` with the same ID more than once is safe — if the subscription already exists, no duplicate is created.

```csharp
// Safe to call on every application startup
await eventStore.Subscriptions.Subscribe(
    "stable-subscription-id",
    "source-event-store");
```

## Accessing Forwarded Events

Events forwarded from a source are placed into the inbox sequence for that source. Access inbox events through any observer (reactor, projection, reducer) targeting `EventSequenceId.Inbox`:

```csharp
[EventType]
public record OrderPlaced(Guid OrderId, decimal Amount);

public class IncomingOrdersReactor : IReactor
{
    public Task Placed(OrderPlaced @event, EventContext context)
    {
        return HandleIncomingOrderAsync(@event.OrderId, @event.Amount);
    }

    Task HandleIncomingOrderAsync(Guid id, decimal amount) => Task.CompletedTask;
}
```

The Kernel routes incoming events to the appropriate `inbox-{sourceEventStore}` event sequence automatically.

## Registering at Startup

A typical pattern is to register all subscriptions once at application startup, so the Kernel always has the full set of subscriptions active:

```csharp
var app = builder.Build();

var eventStore = app.Services.GetRequiredService<IEventStore>();
await eventStore.Subscriptions.Subscribe(
    "orders-from-fulfillment",
    "fulfillment-service",
    builder => builder.WithEventType<ShipmentDispatched>());

await app.RunAsync();
```

## See Also

- [Outbox and Inbox](outbox-inbox.md) — conceptual explanation of how events flow between stores
- [Reactors](../reactors/index.md) — processing forwarded events with reactors
- [Projections](../projections/index.md) — building read models from forwarded events
