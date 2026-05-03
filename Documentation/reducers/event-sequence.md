# Event Sequence

By default, reducers observe the default event log. You can override this by specifying a different event sequence using either the `[EventSequence]` attribute or the `eventSequence` parameter on the `[Reducer]` attribute.

## Using the `[EventSequence]` Attribute

Apply `[EventSequence]` directly to your reducer class to pin it to a specific event sequence:

```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

[EventSequence("outbox")]
public class ShipmentSummaryReducer : IReducerFor<ShipmentSummary>
{
    public ShipmentSummary OnShipmentDispatched(ShipmentDispatched @event, ShipmentSummary? current, EventContext context)
    {
        return new ShipmentSummary(
            @event.TrackingNumber,
            @event.Carrier,
            context.Occurred);
    }
}
```

## Using the `[Reducer]` Attribute

When you are already using the `[Reducer]` attribute to set a custom identifier or other options, use its `eventSequence` parameter instead of adding a separate `[EventSequence]` attribute:

```csharp
using Cratis.Chronicle.Reducers;

[Reducer(id: "shipment-summary", eventSequence: "outbox")]
public class ShipmentSummaryReducer : IReducerFor<ShipmentSummary>
{
    public ShipmentSummary OnShipmentDispatched(ShipmentDispatched @event, ShipmentSummary? current, EventContext context)
    {
        return new ShipmentSummary(
            @event.TrackingNumber,
            @event.Carrier,
            context.Occurred);
    }
}
```

Both approaches produce the same result. Prefer `[Reducer(eventSequence: ...)]` when you are already customizing the reducer with other parameters on that attribute.

## Convenience: `[EventLog]`

Use `[EventLog]` when you want to be explicit that the reducer reads from the default event log, even when event types in the assembly carry a `[EventStore]` attribute pointing elsewhere:

```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

[EventLog]
public class LocalOrderSummaryReducer : IReducerFor<LocalOrderSummary>
{
    public LocalOrderSummary OnOrderPlaced(OrderPlaced @event, LocalOrderSummary? current, EventContext context)
    {
        var count = current?.OrderCount ?? 0;
        return new LocalOrderSummary(count + 1, context.Occurred);
    }
}
```

## Automatic Inbox Routing

When the event types handled by a reducer carry a `[EventStore]` attribute pointing to a **different** event store than the one the reducer is registered in, Chronicle automatically routes the reducer to the corresponding inbox sequence. No `[EventSequence]` annotation is needed in that case.

When the `[EventStore]` attribute points to the **same** event store as the current one, Chronicle routes to the event log instead.

## When to Use `[EventSequence]` Explicitly

- When you need to read from a specific sequence other than the inferred one
- To suppress automatic inbox routing for a reducer that handles foreign events
- For reducers that target a specialized or partitioned event stream
- When an explicit sequence name improves readability and makes intent clear
