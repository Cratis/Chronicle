```csharp title="Combine FromAll with event-specific mappings"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record OrderCreatedDeclarativeAll(string OrderNumber);

[EventType]
public record OrderShippedDeclarativeAll(string TrackingNumber);

public record OrderDeclarativeAll(
    string OrderNumber,
    string Status,
    DateTimeOffset LastModified);

public class OrderDeclarativeAllProjection : IProjectionFor<OrderDeclarativeAll>
{
    public void Define(IProjectionBuilderFor<OrderDeclarativeAll> builder) => builder
        .FromAll(_ => _
            .Set(m => m.LastModified)
            .ToEventContextProperty(c => c.Occurred))
        .From<OrderCreatedDeclarativeAll>(_ => _
            .Set(m => m.Status)
            .ToValue("Placed"))
        .From<OrderShippedDeclarativeAll>(_ => _
            .Set(m => m.Status)
            .ToValue("Shipped"));
}
```
