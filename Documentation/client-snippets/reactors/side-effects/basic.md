```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record SideEffectsBookReserved(string Isbn);

[EventType]
public record StockDecreased(string Isbn, int Quantity);

public class WarehouseReactor : IReactor
{
    public StockDecreased BookReserved(SideEffectsBookReserved @event, EventContext context) =>
        new(@event.Isbn, 1);

    public async Task<StockDecreased> BookReservedAsync(SideEffectsBookReserved @event, EventContext context)
    {
        var available = await FetchCurrentStockAsync(@event.Isbn);
        return new StockDecreased(@event.Isbn, available);
    }

    Task<int> FetchCurrentStockAsync(string isbn) => Task.FromResult(0);
}
```
