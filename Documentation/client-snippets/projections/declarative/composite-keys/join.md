```csharp title="Join with a composite key"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record CompositeProductUpdated(
    string ProductId,
    string Variant,
    string ProductName);

public record CompositeProductKey(string ProductId, string Variant);

public record CompositeOrderLine(
    CompositeProductKey ProductKey,
    string ProductName);

public class CompositeOrderLineProjection : IProjectionFor<CompositeOrderLine>
{
    public void Define(IProjectionBuilderFor<CompositeOrderLine> builder) => builder
        .Join<CompositeProductUpdated>(product => product
            .On(m => m.ProductKey)
            .UsingCompositeKey<CompositeProductKey>(key => key
                .Set(k => k.ProductId).To(e => e.ProductId)
                .Set(k => k.Variant).To(e => e.Variant)));
}
```
