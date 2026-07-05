```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using System.Collections.Immutable;

[EventType]
public record GettingEventsOrderPlaced(string OrderId, decimal Total);

[EventType]
public record GettingEventsOrderCancelled(string OrderId, string Reason);

public class GettingEventsOrderHistoryReader(IEventLog eventLog)
{
    public async Task<IImmutableList<AppendedEvent>> GetOrderEvents(EventSourceId orderId)
    {
        // Filters the timeline to only the order events you care about.
        var eventTypes = new[]
        {
            typeof(GettingEventsOrderPlaced).GetEventType(),
            typeof(GettingEventsOrderCancelled).GetEventType()
        };

        return await eventLog.GetForEventSourceIdAndEventTypes(orderId, eventTypes);
    }
}
```
