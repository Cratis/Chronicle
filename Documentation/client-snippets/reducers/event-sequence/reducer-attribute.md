```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record ReducersEventSequenceReducerAttributeShipmentDispatched(string TrackingNumber, string Carrier);

public record ReducersEventSequenceReducerAttributeShipmentSummary(string TrackingNumber, string Carrier, DateTimeOffset DispatchedAt);

[Reducer(id: "shipment-summary", eventSequence: "fulfillment-events")]
public class ReducersEventSequenceReducerAttributeShipmentSummaryReducer : IReducerFor<ReducersEventSequenceReducerAttributeShipmentSummary>
{
    public ReducersEventSequenceReducerAttributeShipmentSummary Dispatched(ReducersEventSequenceReducerAttributeShipmentDispatched @event, ReducersEventSequenceReducerAttributeShipmentSummary? current, EventContext context) =>
        new(@event.TrackingNumber, @event.Carrier, context.Occurred);
}
```
