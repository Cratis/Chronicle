# Outbox and Inbox

The outbox/inbox pattern is a well-established way to integrate distributed systems reliably. Chronicle formalizes this pattern as first-class, kernel-managed event sequences.

## The Outbox

Every event store in Chronicle has a well-known **outbox** sequence (`EventSequenceId.Outbox`). Events appended to the outbox are visible to any other event store that has set up a subscription.

The outbox exists by convention — you do not need to create it. Append events to it the same way you append to any other event sequence.

## The Inbox

When event store **A** subscribes to event store **B**, Chronicle automatically manages an **inbox** sequence on A for events forwarded from B. The inbox sequence identifier is derived from the source event store name:

```csharp
var inboxId = WellKnownEventSequences.Inbox("source-event-store");
// Resolves to: "inbox-source-event-store"
```

You never write to an inbox directly. Chronicle forwards events from the source outbox to the target inbox as they are appended.

## Lifecycle

Subscriptions are **kernel-managed** and **persistent**. Once registered, a subscription:

- Survives client disconnections
- Is restarted automatically when the Kernel starts
- Is tracked per event store, not per client session

This means you can call `Subscribe` at application startup without worrying about duplicate registration — if the subscription already exists with the same identifier, no new event is appended.

## Event Flow

```text
Source event store               Target event store
─────────────────                ─────────────────
      Outbox                           Inbox
   ┌─────────┐   [subscription]   ┌─────────┐
   │ event A │ ─────────────────▶ │ event A │
   │ event B │ ─────────────────▶ │ event B │
   └─────────┘                    └─────────┘
```

The `EventStoreSubscriptionObserverSubscriber` grain on the Kernel side observes the source outbox and appends forwarded events to the corresponding target inbox.

## Reactors, Projections, and Reducers on the Inbox

Client-side observers (reactors, projections, reducers) can target `EventSequenceId.Inbox` to process all events arriving from subscribed sources. The client SDK automatically routes inbox-targeted observers to the correct per-source inbox sequence.

```csharp
public class IncomingOrdersReactor : IReactor
{
    public Task OrderPlaced(OrderPlaced @event, EventContext context)
    {
        // Handles OrderPlaced events from any subscribed source event store
        return ProcessAsync(@event.OrderId);
    }
}
```

## See Also

- [Implicit Event Store Subscriptions](implicit-subscriptions.md)
- [Explicit Event Store Subscriptions](explicit-subscriptions.md)
