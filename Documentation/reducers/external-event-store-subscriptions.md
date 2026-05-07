# Subscribe Reducers to External Event Stores

## Automatic Inbox Routing

When all event types reduced by a reducer come from the same source event store, Chronicle routes the reducer to the matching inbox sequence automatically:

- Source event store: `fulfillment-service`
- Inbox sequence observed by the reducer: `inbox-fulfillment-service`

This works when the source is declared on event types (or assembly) using `[EventStore("...")]`, and no explicit event sequence is configured on the reducer.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
[EventStore("fulfillment-service")]
public record ShipmentDispatched(Guid OrderId, string TrackingNumber);

public record FulfillmentStatus(string Status, string TrackingNumber);

public class FulfillmentStatusReducer : IReducerFor<FulfillmentStatus>
{
    public FulfillmentStatus Reduce(ShipmentDispatched @event, FulfillmentStatus? current, EventContext context) =>
        new("Dispatched", @event.TrackingNumber);
}
```

## Observer-Level EventStore for Missing Event Metadata

When event contracts cannot carry `[EventStore]` metadata, apply `[EventStore]` directly on the reducer.

Chronicle resolves the reducer to the inbox for that source event store and implicitly creates or reuses the outbox-to-inbox subscription.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record ExternalUserInvited(Guid UserId);

public record ExternalUserTotals(int Count);

[EventStore("identity-service")]
[Reducer]
public class ExternalUserTotalsReducer : IReducerFor<ExternalUserTotals>
{
    public ExternalUserTotals Reduce(ExternalUserInvited @event, ExternalUserTotals? current, EventContext context) =>
        new((current?.Count ?? 0) + 1);
}
```

## Implicit Combined Event Type Filtering

For each source event store, Chronicle combines all reducer and reactor event types into one implicit subscription filter.

This means:

- You get one subscription per source event store.
- The filter is the union of handled event types from all participating observers.
- Only those event types are forwarded from source outbox to target inbox.

## Explicit Event Sequence Is Not Allowed with Observer EventStore

When a reducer has `[EventStore("...")]`, do not combine it with:

- `[EventSequence("...")]`
- `[EventLog]`
- `[Reducer(eventSequence: "...")]`

This is invalid and fails:

- At runtime with `EventStoreCannotBeCombinedWithExplicitEventSequence`.
- At compile time with analyzer rule `CHR0014`.

## See Also

- [Implicit Event Store Subscriptions](../integration/implicit-subscriptions.md)
- [Explicit Event Store Subscriptions](../integration/explicit-subscriptions.md)
- [Event Sequence](event-sequence.md)
