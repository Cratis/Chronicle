```csharp title="Exclude child projection events"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record OrderCreatedDeclarativeEveryExclude(string OrderNumber);

public record OrderAuditDeclarativeEveryExclude(
    string OrderNumber,
    DateTimeOffset LastUpdated);

public class OrderAuditDeclarativeEveryExcludeProjection : IProjectionFor<OrderAuditDeclarativeEveryExclude>
{
    public void Define(IProjectionBuilderFor<OrderAuditDeclarativeEveryExclude> builder) => builder
        .From<OrderCreatedDeclarativeEveryExclude>()
        .FromEvery(_ => _
            .Set(m => m.LastUpdated)
            .ToEventContextProperty(c => c.Occurred)
            .ExcludeChildProjections());
}
```
