```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
[EventStore("fulfillment-service")]
public record ExternalReducerShipmentDispatched(Guid OrderId, string TrackingNumber);

public record ExternalReducerFulfillmentStatus(string Status, string TrackingNumber);

public class ExternalReducerFulfillmentStatusReducer : IReducerFor<ExternalReducerFulfillmentStatus>
{
    public ExternalReducerFulfillmentStatus Dispatched(ExternalReducerShipmentDispatched @event, ExternalReducerFulfillmentStatus? current, EventContext context) =>
        new("Dispatched", @event.TrackingNumber);
}
```
