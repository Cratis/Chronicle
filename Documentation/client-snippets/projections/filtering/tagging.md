```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Projections;

public record FilteringOrderReport(string CustomerId);

// Labels the projection for discoverability — does not affect which events are received
[Tag("reporting")]
public class FilteringOrderReportingProjection : IProjectionFor<FilteringOrderReport>
{
    public void Define(IProjectionBuilderFor<FilteringOrderReport> builder) =>
        builder.From<FilteringOrderPlaced>(b => b.UsingKey(e => e.CustomerId));
}
```
