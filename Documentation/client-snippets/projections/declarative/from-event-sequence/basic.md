```csharp
using Cratis.Chronicle.Projections;

public class DecFromEventSequenceOrderProjection : IProjectionFor<DecFromEventSequenceOrder>
{
    public void Define(IProjectionBuilderFor<DecFromEventSequenceOrder> builder) => builder
        .FromEventSequence("order-management")
        .AutoMap()
        .From<DecFromEventSequenceOrderCreated>()
        .From<DecFromEventSequenceOrderUpdated>()
        .From<DecFromEventSequenceOrderShipped>();
}
```
