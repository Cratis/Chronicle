```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record EventSequenceShipmentDispatched(string TrackingNumber);

[EventSequence("fulfillment-events")]
public class EventSequenceShipmentReactor : IReactor
{
    public Task ShipmentDispatched(EventSequenceShipmentDispatched @event, EventContext context) =>
        NotifyCarrierAsync(@event.TrackingNumber);

    Task NotifyCarrierAsync(string trackingNumber) => Task.CompletedTask;
}
```
