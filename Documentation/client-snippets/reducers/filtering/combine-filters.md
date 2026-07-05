```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

[EventType]
public record ReducersFilteringCombineOrderPlaced(decimal TotalAmount);

public record ReducersFilteringPremiumFulfillmentTotals(int Count, decimal Total);

public class ReducersFilteringCombineOrderService(IEventLog eventLog)
{
    public Task PlacePremiumOrder(decimal totalAmount) =>
        eventLog.Append(
            EventSourceId.New(),
            new ReducersFilteringCombineOrderPlaced(totalAmount),
            tags: ["premium"],
            eventSourceType: "order",
            eventStreamType: "fulfillment");
}

[FilterEventsByTag("premium")]
[EventSourceType("order")]
[EventStreamType("fulfillment")]
public class ReducersFilteringPremiumFulfillmentTotalsReducer : IReducerFor<ReducersFilteringPremiumFulfillmentTotals>
{
    public ReducersFilteringPremiumFulfillmentTotals Placed(ReducersFilteringCombineOrderPlaced @event, ReducersFilteringPremiumFulfillmentTotals? current, EventContext context) =>
        new((current?.Count ?? 0) + 1, (current?.Total ?? 0m) + @event.TotalAmount);
}
```
