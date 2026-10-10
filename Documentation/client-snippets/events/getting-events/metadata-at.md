```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public static class EventMetadataReads
{
    // Call only after your application authorizes access to these events.
    public static async Task Read(IEventSequence eventSequence)
    {
        EventMetadata? metadata = await eventSequence.GetMetadataAt((EventSequenceNumber)42UL);
        if (metadata is not null)
        {
            Console.WriteLine($"{metadata.EventStreamType}: {metadata.CausedBy.Resolution}");
        }

        var batch = await eventSequence.GetMetadataAt([42UL, 43UL]);
        foreach (var entry in batch.Values)
        {
            Console.WriteLine($"{entry.SequenceNumber}: {entry.CausedBy.Name ?? "Name unavailable"}");
        }
    }
}
```
