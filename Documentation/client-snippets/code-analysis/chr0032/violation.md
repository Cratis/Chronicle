```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using MongoDB.Driver;

// Warning CHR0032: Reactor injects storage primitive 'orders' directly; this couples the
// reactor to a sink and bypasses Chronicle's read-model abstraction. Read keyed state
// through an injected read model parameter or IReadModels.GetInstanceById instead.
public class Chr0032OrderProcessor(IMongoCollection<Chr0032Order> orders) : IReactor
{
    public async Task OrderPlaced(Chr0032OrderPlaced @event, EventContext context) =>
        await orders.Find(order => order.OrderNumber == @event.OrderNumber).FirstOrDefaultAsync();
}

[EventType]
public record Chr0032OrderPlaced(string OrderNumber);

public record Chr0032Order(string OrderNumber);
```
