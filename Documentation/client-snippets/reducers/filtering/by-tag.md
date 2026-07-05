```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

[EventType]
public record ReducersFilteringByTagOrderPlaced(decimal TotalAmount);

public record ReducersFilteringPriorityOrderTotals(int Count, decimal Total);

public class ReducersFilteringByTagOrderService(IEventLog eventLog)
{
    public Task PlacePriorityOrder(decimal totalAmount) =>
        eventLog.Append(
            EventSourceId.New(),
            new ReducersFilteringByTagOrderPlaced(totalAmount),
            tags: ["priority"]);
}

[FilterEventsByTag("priority")]
public class ReducersFilteringPriorityOrderTotalsReducer : IReducerFor<ReducersFilteringPriorityOrderTotals>
{
    public ReducersFilteringPriorityOrderTotals Placed(ReducersFilteringByTagOrderPlaced @event, ReducersFilteringPriorityOrderTotals? current, EventContext context) =>
        new((current?.Count ?? 0) + 1, (current?.Total ?? 0m) + @event.TotalAmount);
}
```
