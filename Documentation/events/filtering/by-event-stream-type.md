# Filter reducers and reactors by event stream type

Use `[EventStreamType]` on a reducer or reactor when it should only handle events appended to a specific stream type.

## How the match works

Chronicle compares the observer attribute to the `eventStreamType:` value used when appending the event. If the values do not match, the reducer or reactor is skipped.

## Filter a reactor by event stream type

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
public class PaymentNotificationsReactor : IReactor
{
    public Task Captured(PaymentCaptured @event, EventContext context) => Task.CompletedTask;
}
```

The reactor only handles events appended to the `payments` stream type.

## Filter a reducer by event stream type

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record ShipmentSent(decimal ShippingCost);

public record ShippingTotals(decimal ShippingCost);

[EventStreamType("shipping")]
public class ShippingTotalsReducer : IReducerFor<ShippingTotals>
{
    public ShippingTotals Sent(ShipmentSent @event, ShippingTotals? current, EventContext context) =>
        new((current?.ShippingCost ?? 0m) + @event.ShippingCost);
}

public class ShippingService(IEventLog eventLog)
{
    public Task Send(decimal shippingCost) =>
        eventLog.Append(
            EventSourceId.New(),
            new ShipmentSent(shippingCost),
            eventStreamType: "shipping");
}
```

If the same event is appended with another stream type, such as `eventStreamType: "returns"`, this reducer does not receive it.

## When to choose stream type filtering

Use stream type filtering when the same event source can produce distinct processing flows and you want a reducer or reactor to observe only one of those flows.
