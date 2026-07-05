```csharp title="Composite key in a child collection"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record CompositeOrderCreatedForItems(string CustomerId, string OrderNumber);

[EventType]
public record CompositeItemAddedToOrder(
    string CustomerId,
    string OrderNumber,
    string ProductId,
    string Variant,
    int Quantity);

public record CompositeOrderWithItems(
    CompositeOrderKey Id,
    IEnumerable<CompositeOrderItem> OrderItems);

public record CompositeOrderItem(
    CompositeItemKey Id,
    string ProductId,
    string Variant,
    int Quantity);

public record CompositeItemKey(string ProductId, string Variant);

public class CompositeOrderItemsProjection : IProjectionFor<CompositeOrderWithItems>
{
    public void Define(IProjectionBuilderFor<CompositeOrderWithItems> builder) => builder
        .From<CompositeOrderCreatedForItems>(created => created
            .UsingCompositeKey<CompositeOrderKey>(key => key
                .Set(k => k.CustomerId).To(e => e.CustomerId)
                .Set(k => k.OrderNumber).To(e => e.OrderNumber)))
        .Children(m => m.OrderItems, items => items
            .IdentifiedBy(m => m.Id)
            .From<CompositeItemAddedToOrder>(added => added
                .UsingParentCompositeKey<CompositeOrderKey>(key => key
                    .Set(k => k.CustomerId).To(e => e.CustomerId)
                    .Set(k => k.OrderNumber).To(e => e.OrderNumber))
                .UsingCompositeKey<CompositeItemKey>(key => key
                    .Set(k => k.ProductId).To(e => e.ProductId)
                    .Set(k => k.Variant).To(e => e.Variant))));
}
```
