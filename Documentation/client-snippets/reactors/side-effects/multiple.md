```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record MultipleSideEffectsBookReserved(string Isbn);

[EventType]
public record MultipleStockDecreased(string Isbn, int Quantity);

[EventType]
public record StockLow(string Isbn);

public class InventoryReactor : IReactor
{
    public IEnumerable<object> BookReserved(MultipleSideEffectsBookReserved @event, EventContext context) =>
    [
        new MultipleStockDecreased(@event.Isbn, 1),
        new StockLow(@event.Isbn),
    ];
}
```
