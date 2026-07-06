```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using System.Collections.Immutable;

public class GettingEventsReplayEvents(IEventLog eventLog)
{
    public async Task<IImmutableList<AppendedEvent>> ReadFrom(EventSequenceNumber sequenceNumber)
    {
        // Replays from a known checkpoint to rebuild projections or read models.
        return await eventLog.GetFromSequenceNumber(sequenceNumber);
    }
}
```
