```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
[EventStore("fulfillment-service")]
public record ExternalEventStoreShipmentDispatched(Guid OrderId, string TrackingNumber);

public class ExternalEventStoreFulfillmentReactor : IReactor
{
    public Task ShipmentDispatched(ExternalEventStoreShipmentDispatched @event, EventContext context) => Task.CompletedTask;
}
```
