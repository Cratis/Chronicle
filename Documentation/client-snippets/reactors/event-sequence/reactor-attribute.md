```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record EventSequenceReactorAttributeShipmentDispatched(string TrackingNumber);

[Reactor(id: "shipment-reactor", eventSequence: "fulfillment-events")]
public class EventSequenceReactorAttributeShipmentReactor : IReactor
{
    public Task ShipmentDispatched(EventSequenceReactorAttributeShipmentDispatched @event, EventContext context) =>
        NotifyCarrierAsync(@event.TrackingNumber);

    Task NotifyCarrierAsync(string trackingNumber) => Task.CompletedTask;
}
```
