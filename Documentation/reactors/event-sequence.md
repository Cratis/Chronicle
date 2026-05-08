# Event Sequence

By default, reactors observe the default event log. You can override this by specifying a different event sequence using either the `[EventSequence]` attribute or the `eventSequence` parameter on the `[Reactor]` attribute.

## Using the `[EventSequence]` Attribute

Apply `[EventSequence]` directly to your reactor class to pin it to a specific event sequence:

```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventSequence("fulfillment-events")]
public class ShipmentReactor : IReactor
{
    public Task ShipmentDispatched(ShipmentDispatched @event, EventContext context)
    {
        return NotifyCarrierAsync(@event.TrackingNumber);
    }

    Task NotifyCarrierAsync(string trackingNumber) => Task.CompletedTask;
}
```

## Using the `[Reactor]` Attribute

When you are already using the `[Reactor]` attribute to set a custom identifier or other options, use its `eventSequence` parameter instead of adding a separate `[EventSequence]` attribute:

```csharp
using Cratis.Chronicle.Reactors;

[Reactor(id: "shipment-reactor", eventSequence: "fulfillment-events")]
public class ShipmentReactor : IReactor
{
    public Task ShipmentDispatched(ShipmentDispatched @event, EventContext context)
    {
        return NotifyCarrierAsync(@event.TrackingNumber);
    }

    Task NotifyCarrierAsync(string trackingNumber) => Task.CompletedTask;
}
```

Both approaches produce the same result. Prefer `[Reactor(eventSequence: ...)]` when you are already customizing the reactor with other parameters on that attribute.

## Convenience: `[EventLog]`

Use `[EventLog]` when you want to be explicit that the reactor reads from the default event log, even when event types in the assembly carry a `[EventStore]` attribute pointing elsewhere:

```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventLog]
public class LocalAuditReactor : IReactor
{
    public Task OrderPlaced(OrderPlaced @event, EventContext context)
    {
        return WriteAuditAsync(@event.OrderId, context.Occurred);
    }

    Task WriteAuditAsync(Guid orderId, DateTimeOffset occurred) => Task.CompletedTask;
}
```

## External Event Store Inbox Routing

For outbox-to-inbox subscriptions between event stores, including automatic inbox routing and observer-level `[EventStore]` usage, see [Subscribe Reactors to External Event Stores](external-event-store-subscriptions.md).

## When to Use `[EventSequence]` Explicitly

- When you need to read from a specific sequence other than the inferred one
- To suppress automatic inbox routing for a reactor that handles foreign events
- For reactors that target a specialized or partitioned event stream
- When an explicit sequence name improves readability and makes intent clear
