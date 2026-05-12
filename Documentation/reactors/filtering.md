# Filter reactors by appended event metadata

A reactor observes all events that match its handler method signatures by default. You can restrict which events a reactor receives by placing filter attributes on the reactor class. Chronicle evaluates these filters before dispatching an event — events that do not match are dropped before they ever reach the reactor.

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

Append an event with a tag and place `[FilterEventsByTag]` on the reactor to receive only tagged events.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record OrderPlaced(decimal TotalAmount);

public class OrderService(IEventLog eventLog)
{
    public Task PlacePriorityOrder(decimal totalAmount) =>
        eventLog.Append(
            EventSourceId.New(),
            new OrderPlaced(totalAmount),
            tags: ["priority"]);
}

[FilterEventsByTag("priority")]
public class PriorityOrderNotifier : IReactor
{
    public Task Placed(OrderPlaced @event, EventContext context) =>
        Task.CompletedTask;
}
```

`PriorityOrderNotifier` receives the event because the append call includes the `priority` tag. Orders appended without that tag are not dispatched.

### Multiple filter tags

Multiple `[FilterEventsByTag]` attributes widen the match. The reactor receives the event if the appended event has any of the configured tags:

```csharp
[FilterEventsByTag("priority")]
[FilterEventsByTag("express")]
public class FastTrackOrderNotifier : IReactor
{
    public Task Placed(OrderPlaced @event, EventContext context) =>
        Task.CompletedTask;
}
```

## Filter by event source type

Place `[EventSourceType]` on the reactor to receive only events appended with a matching `eventSourceType`:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record CustomerRegistered(string EmailAddress);

public class CustomerService(IEventLog eventLog)
{
    public Task Register(string emailAddress) =>
        eventLog.Append(
            EventSourceId.New(),
            new CustomerRegistered(emailAddress),
            eventSourceType: "customer");
}

[EventSourceType("customer")]
public class CustomerWelcomeReactor : IReactor
{
    public Task Registered(CustomerRegistered @event, EventContext context) =>
        Task.CompletedTask;
}
```

## Filter by event stream type

Place `[EventStreamType]` on the reactor to receive only events appended to a matching stream type:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record PaymentCaptured(decimal Amount);

public class PaymentsService(IEventLog eventLog)
{
    public Task Capture(decimal amount) =>
        eventLog.Append(
            EventSourceId.New(),
            new PaymentCaptured(amount),
            eventStreamType: "payments");
}

[EventStreamType("payments")]
public class PaymentReceivedNotifier : IReactor
{
    public Task Captured(PaymentCaptured @event, EventContext context) =>
        Task.CompletedTask;
}
```

## Combine multiple filters

You can combine `[FilterEventsByTag]`, `[EventSourceType]`, and `[EventStreamType]` on the same reactor. Chronicle requires all filter categories to match:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record ShipmentDispatched(string TrackingNumber);

public class ShippingService(IEventLog eventLog)
{
    public Task Dispatch(string trackingNumber) =>
        eventLog.Append(
            EventSourceId.New(),
            new ShipmentDispatched(trackingNumber),
            tags: ["express"],
            eventSourceType: "shipment",
            eventStreamType: "logistics");
}

[FilterEventsByTag("express")]
[EventSourceType("shipment")]
[EventStreamType("logistics")]
public class ExpressShipmentNotifier : IReactor
{
    public Task Dispatched(ShipmentDispatched @event, EventContext context) =>
        Task.CompletedTask;
}
```

The reactor only receives events that match all three: the `express` tag, the `shipment` event source type, and the `logistics` stream type.

## Tagging the reactor vs filtering events

`[Tag]` and `[Tags]` on a reactor class label the reactor itself for organization and discoverability. They do not filter incoming events:

```csharp
// These labels appear on the reactor definition — they do not affect dispatch
[Tag("notifications")]
[Tag("express")]
public class ExpressShipmentNotifier : IReactor { }

// These filter which events are dispatched to the reactor
[FilterEventsByTag("express")]
[EventSourceType("shipment")]
public class ExpressShipmentNotifier : IReactor { }
```

## Detailed guides

- [Filter by tag](../events/filtering/by-tag.md)
- [Filter by event source type](../events/filtering/by-event-source-type.md)
- [Filter by event stream type](../events/filtering/by-event-stream-type.md)
