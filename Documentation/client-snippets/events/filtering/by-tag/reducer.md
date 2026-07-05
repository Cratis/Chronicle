```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

[EventType]
public record FilterByTagOrderPlaced(decimal TotalAmount);

public record FilterByTagPriorityOrderTotals(decimal TotalAmount);

[FilterEventsByTag("priority")]
public class FilterByTagPriorityOrderTotalsReducer : IReducerFor<FilterByTagPriorityOrderTotals>
{
    public FilterByTagPriorityOrderTotals Placed(FilterByTagOrderPlaced @event, FilterByTagPriorityOrderTotals? current, EventContext context) =>
        new((current?.TotalAmount ?? 0m) + @event.TotalAmount);
}

public class FilterByTagCheckoutService(IEventLog eventLog)
{
    public Task PlacePriorityOrder(decimal totalAmount) =>
        eventLog.Append(
            EventSourceId.New(),
            new FilterByTagOrderPlaced(totalAmount),
            tags: ["priority"]);
}
```
