# Subscribe Reactors to External Event Stores

## Automatic Inbox Routing

When all event types handled by a reactor come from the same source event store, Chronicle routes the reactor to the matching inbox sequence automatically:

- Source event store: `fulfillment-service`
- Inbox sequence observed by the reactor: `inbox-fulfillment-service`

This works when the source is declared on event types (or assembly) using `[EventStore("...")]`, and no explicit event sequence is configured on the reactor.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
[EventStore("fulfillment-service")]
public record ShipmentDispatched(Guid OrderId, string TrackingNumber);

public class FulfillmentReactor : IReactor
{
    public Task ShipmentDispatched(ShipmentDispatched @event, EventContext context) => Task.CompletedTask;
}
```

## Observer-Level EventStore for Missing Event Metadata

When event contracts cannot carry `[EventStore]` metadata, apply `[EventStore]` directly on the reactor.

Chronicle resolves the reactor to the inbox for that source event store and implicitly creates or reuses the outbox-to-inbox subscription.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record ExternalUserInvited(Guid UserId);

[EventStore("identity-service")]
[Reactor]
public class ExternalUserInvitedReactor : IReactor
{
    public Task On(ExternalUserInvited @event, EventContext context) => Task.CompletedTask;
}
```

## Implicit Combined Event Type Filtering

For each source event store, Chronicle combines all reactor and reducer event types into one implicit subscription filter.

This means:

- You get one subscription per source event store.
- The filter is the union of handled event types from all participating observers.
- Only those event types are forwarded from source outbox to target inbox.

## Explicit Event Sequence Is Not Allowed with Observer EventStore

When a reactor has `[EventStore("...")]`, do not combine it with:

- `[EventSequence("...")]`
- `[EventLog]`
- `[Reactor(eventSequence: "...")]`

This is invalid and fails:

- At runtime with `EventStoreCannotBeCombinedWithExplicitEventSequence`.
- At compile time with analyzer rule `CHR0013`.

## See Also

- [Implicit Event Store Subscriptions](../subscriptions/implicit-subscriptions.md)
- [Explicit Event Store Subscriptions](../subscriptions/explicit-subscriptions.md)
- [Event Sequence](event-sequence.md)
