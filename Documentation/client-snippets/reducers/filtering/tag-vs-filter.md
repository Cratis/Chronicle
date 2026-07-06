```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record ReducersFilteringTagVsFilterOrderPlaced(decimal TotalAmount);

public record ReducersFilteringTagVsFilterTotals(int Count, decimal Total);

// These labels appear on the reducer definition — they do not affect dispatch
[Tag("reporting")]
[Tag("premium")]
public class ReducersFilteringLabeledFulfillmentTotalsReducer : IReducerFor<ReducersFilteringTagVsFilterTotals>
{
    public ReducersFilteringTagVsFilterTotals Placed(ReducersFilteringTagVsFilterOrderPlaced @event, ReducersFilteringTagVsFilterTotals? current, EventContext context) =>
        new((current?.Count ?? 0) + 1, (current?.Total ?? 0m) + @event.TotalAmount);
}

// These filter which events are dispatched to the reducer
[FilterEventsByTag("premium")]
[EventSourceType("order")]
public class ReducersFilteringFilteredFulfillmentTotalsReducer : IReducerFor<ReducersFilteringTagVsFilterTotals>
{
    public ReducersFilteringTagVsFilterTotals Placed(ReducersFilteringTagVsFilterOrderPlaced @event, ReducersFilteringTagVsFilterTotals? current, EventContext context) =>
        new((current?.Count ?? 0) + 1, (current?.Total ?? 0m) + @event.TotalAmount);
}
```
