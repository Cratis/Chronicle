```csharp title="Include child projection events"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record OrderCreatedDeclarativeEveryChildren(string OrderNumber);

[EventType]
public record ItemAddedDeclarativeEveryChildren(string OrderId, string ProductId, string ProductName, int Quantity);

[EventType]
public record ItemQuantityChangedDeclarativeEveryChildren(string OrderId, string ProductId, int Quantity);

public record OrderDeclarativeEveryChildren(
    string OrderNumber,
    DateTimeOffset LastModified,
    IEnumerable<OrderItemDeclarativeEveryChildren> Items);

public record OrderItemDeclarativeEveryChildren(
    string ProductId,
    string ProductName,
    int Quantity);

public class OrderDeclarativeEveryChildrenProjection : IProjectionFor<OrderDeclarativeEveryChildren>
{
    public void Define(IProjectionBuilderFor<OrderDeclarativeEveryChildren> builder) => builder
        .From<OrderCreatedDeclarativeEveryChildren>()
        .FromEvery(_ => _
            .Set(m => m.LastModified)
            .ToEventContextProperty(c => c.Occurred))
        .Children(m => m.Items, children => children
            .IdentifiedBy(m => m.ProductId)
            .From<ItemAddedDeclarativeEveryChildren>(_ => _
                .UsingKey(e => e.ProductId)
                .UsingParentKey(e => e.OrderId))
            .From<ItemQuantityChangedDeclarativeEveryChildren>(_ => _
                .UsingKey(e => e.ProductId)
                .UsingParentKey(e => e.OrderId)));
}
```
