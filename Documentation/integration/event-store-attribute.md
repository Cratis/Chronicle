# Subscribing to Events from External Event Stores

When you publish event types as part of a NuGet package — so other event stores can observe them via the inbox — you should annotate those event types with the `[EventStore]` attribute. This tells Chronicle which event store the events originate from, and allows observers to subscribe to the correct inbox sequence automatically.

## The `[EventStore]` Attribute

Apply `[EventStore]` to any event type that originates from a foreign event store:

```csharp
[EventType]
[EventStore("fulfillment-service")]
public record ShipmentDispatched(Guid OrderId, string TrackingNumber);
```

The string argument is the **name of the source event store** — the same name used when setting up the subscription:

```csharp
await eventStore.Subscriptions.Subscribe(
    "orders-from-fulfillment",
    "fulfillment-service",   // ← must match the EventStore attribute value
    builder => builder.WithEventType<ShipmentDispatched>());
```

## Publishing Event Types in a NuGet Package

A common pattern is to publish your public event contracts as a NuGet package. Consumers add a subscription and reference your package. The `[EventStore]` attribute on each event record provides all the routing information needed without any additional configuration in the consuming project.

A typical public events package looks like this:

```csharp
// FulfillmentService.Events/ShipmentDispatched.cs
using Cratis.Chronicle.Events;

[EventType]
[EventStore("fulfillment-service")]
public record ShipmentDispatched(Guid OrderId, string TrackingNumber);

[EventType]
[EventStore("fulfillment-service")]
public record ShipmentFailed(Guid OrderId, string Reason);
```

All event types in the package share the same `[EventStore]` name.

## Automatic Inbox Routing

When every event type handled by an observer is annotated with the same `[EventStore]`, Chronicle automatically routes that observer to the corresponding inbox sequence — `inbox-{eventStoreName}`. You do not need to specify the event sequence explicitly.

### Reactors

```csharp
// No [Reactor(eventSequence: "inbox-fulfillment-service")] needed
public class FulfillmentReactor : IReactor
{
    public Task ShipmentDispatched(ShipmentDispatched @event, EventContext context)
        => HandleDispatchedAsync(@event.OrderId, @event.TrackingNumber);

    public Task ShipmentFailed(ShipmentFailed @event, EventContext context)
        => HandleFailureAsync(@event.OrderId, @event.Reason);
}
```

### Reducers

```csharp
public class FulfillmentStatusReducer : IReducerFor<FulfillmentStatus>
{
    public FulfillmentStatus Reduce(ShipmentDispatched @event, FulfillmentStatus? current)
        => (current ?? new FulfillmentStatus()) with { Status = "Dispatched", TrackingNumber = @event.TrackingNumber };
}
```

### Projections

```csharp
public class FulfillmentProjection : IProjectionFor<FulfillmentReadModel>
{
    public void Define(IProjectionBuilderFor<FulfillmentReadModel> builder) =>
        builder
            .From<ShipmentDispatched>(_ => _
                .Set(m => m.TrackingNumber).To(e => e.TrackingNumber));
}
```

## Mixing Event Stores Is Not Allowed

An observer may only handle event types from a **single** event store. Mixing types annotated with different `[EventStore]` values on the same observer throws `MultipleEventStoresDefined` at startup:

```csharp
// ❌ This will throw MultipleEventStoresDefined
public class InvalidReactor : IReactor
{
    // ShipmentDispatched has [EventStore("fulfillment-service")]
    public Task Handle(ShipmentDispatched @event) => Task.CompletedTask;

    // OrderPlaced has [EventStore("ordering-service")]
    public Task Handle(OrderPlaced @event) => Task.CompletedTask;
}
```

Create a separate observer for each source event store.

## Schema Registration Metadata

When event types are registered with the Kernel, the source event store name is included in the registration metadata. This allows the Kernel to associate incoming inbox events with their originating store and display that information in the Workbench.

## See Also

- [Event Store Subscriptions](event-store-subscriptions.md) — setting up subscriptions between event stores
- [Outbox and Inbox](outbox-inbox.md) — how events flow between stores
