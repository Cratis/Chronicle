```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record ScenariosReactBookReserved(string Isbn);

[EventType]
public record ScenariosReactStockDecreased(string Isbn, int Quantity);

public class ScenariosReactStockKeeping : IReactor
{
    // Without [OnceOnly], a replay of this reactor appends StockDecreased again.
    [OnceOnly]
    public ScenariosReactStockDecreased BookReserved(ScenariosReactBookReserved @event, EventContext context) =>
        new(@event.Isbn, 1);
}
```
