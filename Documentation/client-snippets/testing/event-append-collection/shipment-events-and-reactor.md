```csharp
namespace Cratis.Chronicle.Docs.EventAppendCollection
{
    [EventType]
    public record OrderPlaced(string OrderId);

    [EventType]
    public record ShipmentScheduled(string OrderId);

    public class ShipmentReactor(IEventLog eventLog) : IReactor
    {
        public Task OnOrderPlaced(OrderPlaced evt, EventContext ctx) =>
            eventLog.Append(ctx.EventSourceId, new ShipmentScheduled(evt.OrderId));
    }
}
```
