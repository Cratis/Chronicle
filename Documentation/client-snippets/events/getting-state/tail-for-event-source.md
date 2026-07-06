```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
public record GettingStateInventoryAdjusted(string Sku, int Delta);

[EventType]
public record GettingStateInventoryReserved(string Sku, int Quantity);

public class GettingStateInventoryCheckpoint(IEventLog eventLog)
{
    public async Task<EventSequenceNumber> CaptureFor(EventSourceId inventoryId)
    {
        // Scopes the tail to a specific stream of inventory events.
        var eventTypes = new[]
        {
            typeof(GettingStateInventoryAdjusted).GetEventType(),
            typeof(GettingStateInventoryReserved).GetEventType()
        };

        return await eventLog.GetTailSequenceNumber(
            eventSourceId: inventoryId,
            filterEventTypes: eventTypes
        );
    }
}
```
