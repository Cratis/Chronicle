```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class WarehouseEventSourceReactor(string warehouseId) : IReactor, ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => warehouseId;

    public StockDecreased BookReserved(SideEffectsBookReserved @event, EventContext context) =>
        new(@event.Isbn, 1);
}
```
