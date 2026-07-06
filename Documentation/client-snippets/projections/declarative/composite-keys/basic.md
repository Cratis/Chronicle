```csharp title="Composite key projection"
using Cratis.Chronicle.Projections;

public class CompositeOrderProjection : IProjectionFor<CompositeOrder>
{
    public void Define(IProjectionBuilderFor<CompositeOrder> builder) => builder
        .From<CompositeOrderCreated>(created => created
            .UsingCompositeKey<CompositeOrderKey>(key => key
                .Set(k => k.CustomerId).To(e => e.CustomerId)
                .Set(k => k.OrderNumber).To(e => e.OrderNumber)))
        .From<CompositeOrderShipped>(shipped => shipped
            .UsingCompositeKey<CompositeOrderKey>(key => key
                .Set(k => k.CustomerId).To(e => e.CustomerId)
                .Set(k => k.OrderNumber).To(e => e.OrderNumber)));
}
```
