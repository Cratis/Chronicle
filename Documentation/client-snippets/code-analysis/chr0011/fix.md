```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

public record ProductInventory(string Name, int Stock);

[EventType("product-added")]
[EventStore("catalog")]
public record Chr0011FixProductAdded(string Name, int InitialStock);

[EventType("product-restocked")]
[EventStore("catalog")]
public record Chr0011FixProductRestocked(int Quantity);

// All event types belong to the same event store: "catalog".
public class Chr0011FixProductInventoryProjection : IProjectionFor<ProductInventory>
{
    public void Define(IProjectionBuilderFor<ProductInventory> builder)
    {
        builder
            .From<Chr0011FixProductAdded>()
            .From<Chr0011FixProductRestocked>();
    }
}
```
