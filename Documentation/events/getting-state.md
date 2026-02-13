# Getting state

Event sequence state provides information about how far a sequence has progressed. The most common state value is the tail sequence number, which represents the latest event appended to the sequence. Use the `IEventSequence` APIs, such as `GetTailSequenceNumber` and related state calls, to capture the current position.

Use sequence state for scenarios such as:

- Tracking progress for consumers and observers
- Capturing a point in time for read model time travel
- Avoiding duplicate processing when resuming work

For point-in-time reads of read models, capture the sequence position from the event sequence state and use it alongside the read model APIs described in the read models guides.

Related reading:

- [Getting a Collection of Instances](../read-models/getting-collection-instances.md)

## Examples

### Capture the tail for a checkpoint

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public class CheckpointStore(IEventLog eventLog)
{
    public async Task<EventSequenceNumber> CaptureTail()
    {
        // Persists the current tail so processing can resume later.
        return await eventLog.GetTailSequenceNumber();
    }
}
```

### Capture the tail for a specific event source and event types

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
public record InventoryAdjusted(string Sku, int Delta);

[EventType]
public record InventoryReserved(string Sku, int Quantity);

public class InventoryCheckpoint(IEventLog eventLog)
{
    public async Task<EventSequenceNumber> CaptureFor(EventSourceId inventoryId)
    {
        // Scopes the tail to a specific stream of inventory events.
        var eventTypes = new[]
        {
            typeof(InventoryAdjusted).GetEventType(),
            typeof(InventoryReserved).GetEventType()
        };

        return await eventLog.GetTailSequenceNumber(
            eventSourceId: inventoryId,
            filterEventTypes: eventTypes
        );
    }
}
```

### Check whether an observer is caught up

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public class ObserverProgress(IEventSequence eventSequence)
{
    public async Task<EventSequenceNumber> GetRelevantTail(Type observerType)
    {
        // Uses the observer's event type filters to compute the relevant tail.
        return await eventSequence.GetTailSequenceNumberForObserver(observerType);
    }
}
```

