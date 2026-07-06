```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecConstantKeyPageViewed(string PageUrl);

[EventType]
public record DecConstantKeyButtonClicked(string ButtonId);

[EventType]
public record DecConstantKeyFormSubmitted(string FormId);

public record DecConstantKeyEngagementMetrics(
    int PageViews,
    int ButtonClicks,
    int FormSubmissions);

public class DecConstantKeyEngagementMetricsProjection : IProjectionFor<DecConstantKeyEngagementMetrics>
{
    public void Define(IProjectionBuilderFor<DecConstantKeyEngagementMetrics> builder) => builder
        .From<DecConstantKeyPageViewed>(_ => _
            .UsingConstantKey("metrics")
            .Count(m => m.PageViews))
        .From<DecConstantKeyButtonClicked>(_ => _
            .UsingConstantKey("metrics")
            .Count(m => m.ButtonClicks))
        .From<DecConstantKeyFormSubmitted>(_ => _
            .UsingConstantKey("metrics")
            .Count(m => m.FormSubmissions));
}
```
