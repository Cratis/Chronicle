```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record ReactorsFilteringTagVsFilterShipmentDispatched(string TrackingNumber);

// These labels appear on the reactor definition — they do not affect dispatch
[Tag("notifications")]
[Tag("express")]
public class ReactorsFilteringLabeledShipmentNotifier : IReactor
{
    public Task Dispatched(ReactorsFilteringTagVsFilterShipmentDispatched @event, EventContext context) =>
        Task.CompletedTask;
}

// These filter which events are dispatched to the reactor
[FilterEventsByTag("express")]
[EventSourceType("shipment")]
public class ReactorsFilteringFilteredShipmentNotifier : IReactor
{
    public Task Dispatched(ReactorsFilteringTagVsFilterShipmentDispatched @event, EventContext context) =>
        Task.CompletedTask;
}
```
