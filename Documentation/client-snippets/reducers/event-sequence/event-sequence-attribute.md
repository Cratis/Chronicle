```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

[EventType]
public record ReducersEventSequenceShipmentDispatched(string TrackingNumber, string Carrier);

public record ReducersEventSequenceShipmentSummary(string TrackingNumber, string Carrier, DateTimeOffset DispatchedAt);

[EventSequence("fulfillment-events")]
public class ReducersEventSequenceShipmentSummaryReducer : IReducerFor<ReducersEventSequenceShipmentSummary>
{
    public ReducersEventSequenceShipmentSummary Dispatched(ReducersEventSequenceShipmentDispatched @event, ReducersEventSequenceShipmentSummary? current, EventContext context) =>
        new(@event.TrackingNumber, @event.Carrier, context.Occurred);
}
```
