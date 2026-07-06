```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecConstantKeyOrderPlaced(decimal Total);

public record DecConstantKeyGlobalCounter(int TotalOrders);

public class DecConstantKeyGlobalCounterProjection : IProjectionFor<DecConstantKeyGlobalCounter>
{
    public void Define(IProjectionBuilderFor<DecConstantKeyGlobalCounter> builder) => builder
        .From<DecConstantKeyOrderPlaced>(_ => _
            .UsingConstantKey("global")
            .Count(m => m.TotalOrders));
}
```
