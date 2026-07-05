```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record ReducersFilteringMultiTagOrderPlaced(decimal TotalAmount);

public record ReducersFilteringFastTrackOrderTotals(int Count);

[FilterEventsByTag("priority")]
[FilterEventsByTag("express")]
public class ReducersFilteringFastTrackOrderTotalsReducer : IReducerFor<ReducersFilteringFastTrackOrderTotals>
{
    public ReducersFilteringFastTrackOrderTotals Placed(ReducersFilteringMultiTagOrderPlaced @event, ReducersFilteringFastTrackOrderTotals? current, EventContext context) =>
        new((current?.Count ?? 0) + 1);
}
```
