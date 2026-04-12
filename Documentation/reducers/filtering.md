# Filter reducers by appended event metadata

A reducer observes all events that match its handler method signatures by default. You can restrict which events a reducer receives by placing filter attributes on the reducer class. Chronicle evaluates these filters before dispatching an event — events that do not match are dropped before they ever reach the reducer.

## Filter attributes

| Attribute | Filters by | Matches when |
|---|---|---|
| `[FilterEventsByTag("...")]` | Appended or static event tag | Any filter tag matches any tag on the appended event |
| `[EventSourceType("...")]` | Event source type set at append time | The event source type matches exactly |
| `[EventStreamType("...")]` | Event stream type set at append time | The event stream type matches exactly |

These attributes correlate directly to the metadata you provide when appending an event:

```csharp
await eventLog.Append(
    EventSourceId.New(),
    new OrderPlaced(42m),
    tags: ["priority"],
    eventSourceType: "order",
    eventStreamType: "fulfillment");
```

## Filter by tag

Append an event with a tag and place `[FilterEventsByTag]` on the reducer to accumulate state only from tagged events.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record OrderPlaced(decimal TotalAmount);

public record PriorityOrderTotals(int Count, decimal Total);

public class OrderService(IEventLog eventLog)
{
    public Task PlacePriorityOrder(decimal totalAmount) =>
        eventLog.Append(
            EventSourceId.New(),
            new OrderPlaced(totalAmount),
            tags: ["priority"]);
}

[FilterEventsByTag("priority")]
public class PriorityOrderTotalsReducer : IReducerFor<PriorityOrderTotals>
{
    public PriorityOrderTotals Placed(OrderPlaced @event, PriorityOrderTotals? current, EventContext context) =>
        new((current?.Count ?? 0) + 1, (current?.Total ?? 0m) + @event.TotalAmount);
}
```

`PriorityOrderTotalsReducer` updates only when the appended event carries the `priority` tag. Orders without that tag do not affect this read model.

### Multiple filter tags

Multiple `[FilterEventsByTag]` attributes widen the match. The reducer receives the event if the appended event has any of the configured tags:

```csharp
[FilterEventsByTag("priority")]
[FilterEventsByTag("express")]
public class FastTrackOrderTotalsReducer : IReducerFor<FastTrackOrderTotals>
{
    public FastTrackOrderTotals Placed(OrderPlaced @event, FastTrackOrderTotals? current, EventContext context) =>
        new((current?.Count ?? 0) + 1);
}
```

## Filter by event source type

Place `[EventSourceType]` on the reducer to accumulate state only from events appended with a matching `eventSourceType`:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record InvoiceIssued(decimal Amount);

public record CustomerInvoiceTotal(decimal Amount);

public class InvoicingService(IEventLog eventLog)
{
    public Task IssueCustomerInvoice(decimal amount) =>
        eventLog.Append(
            EventSourceId.New(),
            new InvoiceIssued(amount),
            eventSourceType: "customer");
}

[EventSourceType("customer")]
public class CustomerInvoiceTotalReducer : IReducerFor<CustomerInvoiceTotal>
{
    public CustomerInvoiceTotal Issued(InvoiceIssued @event, CustomerInvoiceTotal? current, EventContext context) =>
        new((current?.Amount ?? 0m) + @event.Amount);
}
```

If the same event is appended with `eventSourceType: "supplier"`, this reducer does not receive it.

## Filter by event stream type

Place `[EventStreamType]` on the reducer to accumulate state only from events appended to a matching stream type:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record ShipmentSent(decimal ShippingCost);

public record ShippingTotals(int Count, decimal TotalCost);

public class ShippingService(IEventLog eventLog)
{
    public Task Send(decimal shippingCost) =>
        eventLog.Append(
            EventSourceId.New(),
            new ShipmentSent(shippingCost),
            eventStreamType: "shipping");
}

[EventStreamType("shipping")]
public class ShippingTotalsReducer : IReducerFor<ShippingTotals>
{
    public ShippingTotals Sent(ShipmentSent @event, ShippingTotals? current, EventContext context) =>
        new((current?.Count ?? 0) + 1, (current?.TotalCost ?? 0m) + @event.ShippingCost);
}
```

## Combine multiple filters

You can combine `[FilterEventsByTag]`, `[EventSourceType]`, and `[EventStreamType]` on the same reducer. Chronicle requires all filter categories to match:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record OrderPlaced(decimal TotalAmount);

public record PremiumFulfillmentTotals(int Count, decimal Total);

public class OrderService(IEventLog eventLog)
{
    public Task PlacePremiumOrder(decimal totalAmount) =>
        eventLog.Append(
            EventSourceId.New(),
            new OrderPlaced(totalAmount),
            tags: ["premium"],
            eventSourceType: "order",
            eventStreamType: "fulfillment");
}

[FilterEventsByTag("premium")]
[EventSourceType("order")]
[EventStreamType("fulfillment")]
public class PremiumFulfillmentTotalsReducer : IReducerFor<PremiumFulfillmentTotals>
{
    public PremiumFulfillmentTotals Placed(OrderPlaced @event, PremiumFulfillmentTotals? current, EventContext context) =>
        new((current?.Count ?? 0) + 1, (current?.Total ?? 0m) + @event.TotalAmount);
}
```

The reducer only receives events that match all three: the `premium` tag, the `order` event source type, and the `fulfillment` stream type.

## Tagging the reducer vs filtering events

`[Tag]` and `[Tags]` on a reducer class label the reducer itself for organization and discoverability. They do not filter incoming events:

```csharp
// These labels appear on the reducer definition — they do not affect dispatch
[Tag("reporting")]
[Tag("premium")]
public class PremiumFulfillmentTotalsReducer : IReducerFor<PremiumFulfillmentTotals> { }

// These filter which events are dispatched to the reducer
[FilterEventsByTag("premium")]
[EventSourceType("order")]
public class PremiumFulfillmentTotalsReducer : IReducerFor<PremiumFulfillmentTotals> { }
```

## Detailed guides

- [Filter by tag](../events/filtering/by-tag.md)
- [Filter by event source type](../events/filtering/by-event-source-type.md)
- [Filter by event stream type](../events/filtering/by-event-stream-type.md)
