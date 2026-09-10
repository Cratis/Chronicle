```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

public record Chr0011ViolationProductInventory(string Name, int Stock);

[EventType("product-added")]
[EventStore("catalog")]
public record Chr0011ViolationProductAdded(string Name, int InitialStock);

[EventType("stock-received")]
[EventStore("warehouse")]
public record Chr0011ViolationStockReceived(int Quantity);

// Error CHR0011: Declarative projection references event types from multiple event stores:
// "catalog", "warehouse". All event types in a projection must originate from the same event store.
public class Chr0011ViolationProductInventoryProjection : IProjectionFor<Chr0011ViolationProductInventory>
{
    public void Define(IProjectionBuilderFor<Chr0011ViolationProductInventory> builder)
    {
        builder
            .From<Chr0011ViolationProductAdded>()
            .From<Chr0011ViolationStockReceived>();
    }
}
```
