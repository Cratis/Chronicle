```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using System.Collections.Immutable;
using System.Linq;

public class GettingEventsTailReader(IEventLog eventLog)
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
