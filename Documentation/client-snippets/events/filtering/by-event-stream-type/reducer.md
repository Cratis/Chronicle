```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

[EventType]
public record FilterByStreamTypeShipmentSent(decimal ShippingCost);

public record FilterByStreamTypeShippingTotals(decimal ShippingCost);

[EventStreamType("shipping")]
public class FilterByStreamTypeShippingTotalsReducer : IReducerFor<FilterByStreamTypeShippingTotals>
{
    public FilterByStreamTypeShippingTotals Sent(FilterByStreamTypeShipmentSent @event, FilterByStreamTypeShippingTotals? current, EventContext context) =>
        new((current?.ShippingCost ?? 0m) + @event.ShippingCost);
}

public class FilterByStreamTypeShippingService(IEventLog eventLog)
{
    public Task Send(decimal shippingCost) =>
        eventLog.Append(
            EventSourceId.New(),
            new FilterByStreamTypeShipmentSent(shippingCost),
            eventStreamType: "shipping");
}
```
