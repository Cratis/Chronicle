```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventStreamType("warehouse")]
[EventSourceType("product")]
public class WarehouseMetadataReactor : IReactor
{
    public StockDecreased BookReserved(SideEffectsBookReserved @event, EventContext context) =>
        new(@event.Isbn, 1);
}
```
