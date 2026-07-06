```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public record FilteringPremiumOrderTotals(int Count, decimal Total);

[FilterEventsByTag("premium")]
public class FilteringPremiumOrderTotalsReducer : IReducerFor<FilteringPremiumOrderTotals>
{
    public FilteringPremiumOrderTotals Placed(FilteringWithReactorOrderPlaced @event, FilteringPremiumOrderTotals? current, EventContext context) =>
        new((current?.Count ?? 0) + 1, (current?.Total ?? 0m) + @event.TotalAmount);
}
```
