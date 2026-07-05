```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecFunctionsItemAdded(string Name);

[EventType]
public record DecFunctionsItemRemoved(string Name);

public record DecFunctionsInventory(int Quantity);

public class DecFunctionsInventoryProjection : IProjectionFor<DecFunctionsInventory>
{
    public void Define(IProjectionBuilderFor<DecFunctionsInventory> builder) => builder
        .AutoMap()
        .From<DecFunctionsItemAdded>(_ => _
            .Increment(m => m.Quantity))
        .From<DecFunctionsItemRemoved>(_ => _
            .Decrement(m => m.Quantity));
}
```
