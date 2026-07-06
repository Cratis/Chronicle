```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record TaggingKpiRecorded(string Kpi, decimal Value);

public record TaggingExecutiveDashboard(string Kpi, decimal Value);

[Tag("Analytics", "Reporting")]
[Tag("Executive")]
public class TaggingExecutiveDashboardProjection : IProjectionFor<TaggingExecutiveDashboard>
{
    public void Define(IProjectionBuilderFor<TaggingExecutiveDashboard> builder) => builder
        .From<TaggingKpiRecorded>(_ => _
            .Set(m => m.Kpi).To(e => e.Kpi)
            .Set(m => m.Value).To(e => e.Value));
}
```
