```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

[EventType]
public record ReducersFilteringShipmentSent(decimal ShippingCost);

public record ReducersFilteringShippingTotals(int Count, decimal TotalCost);

public class ReducersFilteringShippingService(IEventLog eventLog)
{
    public Task Send(decimal shippingCost) =>
        eventLog.Append(
            EventSourceId.New(),
            new ReducersFilteringShipmentSent(shippingCost),
            eventStreamType: "shipping");
}

[EventStreamType("shipping")]
public class ReducersFilteringShippingTotalsReducer : IReducerFor<ReducersFilteringShippingTotals>
{
    public ReducersFilteringShippingTotals Sent(ReducersFilteringShipmentSent @event, ReducersFilteringShippingTotals? current, EventContext context) =>
        new((current?.Count ?? 0) + 1, (current?.TotalCost ?? 0m) + @event.ShippingCost);
}
```
