# Appended event metadata and projections

Projections build read models by mapping events to fields. They observe all events of the types declared in their definition — they do not use `[FilterEventsByTag]`, `[EventSourceType]`, or `[EventStreamType]` to filter incoming events.

## How projections select their input

A projection declares the event types it observes through its event mappings:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record OrderPlaced(string CustomerId, decimal TotalAmount);

[EventType]
public record OrderShipped(DateTimeOffset ShippedAt);

[ReadModel]
[FromEvent<OrderPlaced>]
[FromEvent<OrderShipped>]
public record OrderSummary(
    [Key] string CustomerId,
    decimal TotalAmount,
    DateTimeOffset? ShippedAt);
```

This projection receives every `OrderPlaced` and `OrderShipped` event regardless of any metadata attached during append. Metadata such as tags or stream type does not affect which events flow into a projection.

## Tagging projections

`[Tag]` and `[Tags]` on a projection label the projection definition for organizational purposes. They do not filter incoming events:

```csharp
// Labels the projection for discoverability — does not affect which events are received
[Tag("reporting")]
public class OrderReportingProjection : IProjectionFor<OrderReport>
{
    public void Define(IProjectionBuilderFor<OrderReport> builder) =>
        builder.From<OrderPlaced>(b => b.UsingKey(e => e.CustomerId));
}
```

## Combining a projection with metadata-based filtering

When you need a side effect or secondary read model that reacts only to a subset of events based on appended metadata, pair the projection with a reactor or reducer that carries the appropriate filter attributes.

The following example shows an `OrderSummaryProjection` that builds the full read model for all orders, alongside a `PremiumOrderNotifier` reactor that fires only when an order is appended with the `premium` tag:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reactors;

[EventType]
public record OrderPlaced(string CustomerId, decimal TotalAmount);

// --- Append call ---
// Carries the "premium" tag for orders that qualify
// eventLog.Append(orderId, new OrderPlaced(customerId, total), tags: ["premium"]);

// --- Projection: receives every OrderPlaced ---
[ReadModel]
[FromEvent<OrderPlaced>]
public record OrderSummary(
    [Key] string CustomerId,
    decimal TotalAmount);

// --- Reactor: receives only premium-tagged OrderPlaced ---
[FilterEventsByTag("premium")]
public class PremiumOrderNotifier : IReactor
{
    public Task Placed(OrderPlaced @event, EventContext context) =>
        Task.CompletedTask;
}
```

The same pattern works with a reducer instead of a reactor:

```csharp
using Cratis.Chronicle.Reducers;

public record PremiumOrderTotals(int Count, decimal Total);

[FilterEventsByTag("premium")]
public class PremiumOrderTotalsReducer : IReducerFor<PremiumOrderTotals>
{
    public PremiumOrderTotals Placed(OrderPlaced @event, PremiumOrderTotals? current, EventContext context) =>
        new((current?.Count ?? 0) + 1, (current?.Total ?? 0m) + @event.TotalAmount);
}
```

Use this pattern whenever you need both a projection-based read model (covering all events) and a metadata-filtered view or side effect (covering a subset).

## Appending with metadata

The metadata you provide at append time drives the filters on any accompanying reducers or reactors:

```csharp
// Appends to all observers — no extra metadata
await eventLog.Append(EventSourceId.New(), new OrderPlaced(customerId, 42m));

// Appends to all observers; additionally dispatched to observers filtering on "premium"
await eventLog.Append(EventSourceId.New(), new OrderPlaced(customerId, 299m), tags: ["premium"]);

// Appends with stream type; dispatched to observers filtering on "wholesale" stream type
await eventLog.Append(EventSourceId.New(), new OrderPlaced(customerId, 1500m), eventStreamType: "wholesale");
```

## See also

- [Filter reactors by appended event metadata](../reactors/filtering.md)
- [Filter reducers by appended event metadata](../reducers/filtering.md)
- [Tagging Projections](tagging-projections.md)
