```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record TaggingOrderPlaced(string OrderId);

[EventType]
public record TaggingItemAddedToOrder(decimal Amount);

public record TaggingOrderAnalytics(string OrderId, decimal TotalAmount);

[Tag("Analytics")]
public class TaggingOrderAnalyticsProjection : IProjectionFor<TaggingOrderAnalytics>
{
    public void Define(IProjectionBuilderFor<TaggingOrderAnalytics> builder) => builder
        .From<TaggingOrderPlaced>(_ => _
            .Set(m => m.OrderId).To(e => e.OrderId))
        .From<TaggingItemAddedToOrder>(_ => _
            .Add(m => m.TotalAmount).With(e => e.Amount));
}
```
