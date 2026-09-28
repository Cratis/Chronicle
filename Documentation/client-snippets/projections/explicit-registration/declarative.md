```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record StockReceived(string Sku, int Quantity);

[EventType]
public record StockPicked(string Sku, int Quantity);

public record WarehouseStock(string Sku, int Quantity);

public class WarehouseStockRegistration
{
    public async Task<WarehouseStock?> Register(IEventStore eventStore, string sku)
    {
        await eventStore.Projections.Register<WarehouseStock>(projection => projection
            .From<StockReceived>(from => from.Add(model => model.Quantity).With(@event => @event.Quantity))
            .From<StockPicked>(from => from.Subtract(model => model.Quantity).With(@event => @event.Quantity)));

        // The read model is now an ordinary read model - read it like any other.
        return await eventStore.ReadModels.GetInstanceById<WarehouseStock>(sku);
    }
}
```
