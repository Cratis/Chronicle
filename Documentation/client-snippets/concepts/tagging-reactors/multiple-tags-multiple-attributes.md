```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record TaggingReactorsProductStockChanged(string ProductId, int NewQuantity);

public interface ITaggingReactorsInventoryApi
{
    Task UpdateStock(string productId, int newQuantity);
}

[Tag("Integration")]
[Tag("ExternalAPI")]
[Tag("Inventory")]
public class TaggingReactorsInventorySyncReactor(ITaggingReactorsInventoryApi inventoryApi) : IReactor
{
    public Task StockChanged(TaggingReactorsProductStockChanged @event, EventContext context) =>
        inventoryApi.UpdateStock(@event.ProductId, @event.NewQuantity);
}
```
