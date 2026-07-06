```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record TaggingProductRegistered(Guid ProductId, string Name, int QuantityInStock, decimal UnitPrice);

[Tag("Inventory", "Operations")]
[FromEvent<TaggingProductRegistered>(key: nameof(TaggingProductRegistered.ProductId))]
public record TaggingProductInventory(
    [Key] Guid ProductId,
    string Name,
    int QuantityInStock,
    decimal UnitPrice);
```
