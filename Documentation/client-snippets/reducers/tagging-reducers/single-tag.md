```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record TaggingReducersOrderPlaced(decimal TotalAmount);

public record TaggingReducersOrderAnalytics(int OrderCount, decimal TotalAmount);

[Tag("Analytics")]
public class TaggingReducersOrderAnalyticsReducer : IReducerFor<TaggingReducersOrderAnalytics>
{
    public TaggingReducersOrderAnalytics Placed(TaggingReducersOrderPlaced @event, TaggingReducersOrderAnalytics? current, EventContext context) =>
        new((current?.OrderCount ?? 0) + 1, (current?.TotalAmount ?? 0m) + @event.TotalAmount);
}
```
