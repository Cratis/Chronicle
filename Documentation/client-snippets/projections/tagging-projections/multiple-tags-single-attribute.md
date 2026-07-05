```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record TaggingSaleRecorded(string ProductId, decimal Amount);

public record TaggingSalesReport(string ProductId, decimal TotalSales);

[Tag("Analytics", "Reporting", "Dashboard")]
public class TaggingSalesReportProjection : IProjectionFor<TaggingSalesReport>
{
    public void Define(IProjectionBuilderFor<TaggingSalesReport> builder) => builder
        .From<TaggingSaleRecorded>(_ => _
            .Set(m => m.ProductId).To(e => e.ProductId)
            .Add(m => m.TotalSales).With(e => e.Amount));
}
```
