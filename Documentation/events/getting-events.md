# Getting events

Event sequences support multiple ways of reading events depending on your needs. The `IEventSequence` APIs (and the specialized `IEventLog`) cover common patterns such as:

- Reading events from a sequence in order
- Reading events for a specific event source
- Reading a range of events based on sequence numbers
- Reading a fixed number of events from the tail

Use the client APIs to select the pattern that best matches your scenario, such as replaying state, building read models, or auditing changes.

## Examples

### Read events for a single event source

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using System.Collections.Immutable;

[EventType]
public record OrderPlaced(string OrderId, decimal Total);

[EventType]
public record OrderCancelled(string OrderId, string Reason);

public class OrderHistoryReader(IEventLog eventLog)
{
    public async Task<IImmutableList<AppendedEvent>> GetOrderEvents(EventSourceId orderId)
    {
        // Filters the timeline to only the order events you care about.
        var eventTypes = new[]
        {
            typeof(OrderPlaced).GetEventType(),
            typeof(OrderCancelled).GetEventType()
        };

        return await eventLog.GetForEventSourceIdAndEventTypes(orderId, eventTypes);
    }
}
```

### Read from a checkpoint

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using System.Collections.Immutable;

public class ReplayEvents(IEventLog eventLog)
{
    public async Task<IImmutableList<AppendedEvent>> ReadFrom(EventSequenceNumber sequenceNumber)
    {
        // Replays from a known checkpoint to rebuild projections or read models.
        return await eventLog.GetFromSequenceNumber(sequenceNumber);
    }
}
```

### Read the last events in a sequence

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using System.Collections.Immutable;
using System.Linq;

public class TailReader(IEventLog eventLog)
{
    public async Task<IImmutableList<AppendedEvent>> ReadLast(int count)
    {
        // Reads from the computed start and trims in memory to the requested count.
        var tail = await eventLog.GetTailSequenceNumber();
        var start = tail.IsActualValue && tail.Value >= (ulong)count
            ? tail - (count - 1)
            : EventSequenceNumber.First;

        var events = await eventLog.GetFromSequenceNumber(start);
        return events.TakeLast(count).ToImmutableList();
    }
}
```

