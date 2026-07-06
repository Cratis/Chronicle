```csharp
[EventType]
public record DecNotRewindableApiRequestCompleted(string Endpoint, int StatusCode, long DurationMilliseconds);

public record DecNotRewindablePerformanceMetric(DateTimeOffset Timestamp);

public class DecNotRewindablePerformanceMetricProjection : IProjectionFor<DecNotRewindablePerformanceMetric>
{
    public void Define(IProjectionBuilderFor<DecNotRewindablePerformanceMetric> builder) => builder
        .NotRewindable()
        .AutoMap()
        .From<DecNotRewindableApiRequestCompleted>(_ => _
            .Set(m => m.Timestamp).ToEventContextProperty(c => c.Occurred));
}
```
