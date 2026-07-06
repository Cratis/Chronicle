```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record ReactorsFilteringShipmentDispatched(string TrackingNumber);

public class ReactorsFilteringShippingService(IEventLog eventLog)
{
    public Task Dispatch(string trackingNumber) =>
        eventLog.Append(
            EventSourceId.New(),
            new ReactorsFilteringShipmentDispatched(trackingNumber),
            tags: ["express"],
            eventSourceType: "shipment",
            eventStreamType: "logistics");
}

[FilterEventsByTag("express")]
[EventSourceType("shipment")]
[EventStreamType("logistics")]
public class ReactorsFilteringExpressShipmentNotifier : IReactor
{
    public Task Dispatched(ReactorsFilteringShipmentDispatched @event, EventContext context) =>
        Task.CompletedTask;
}
```
