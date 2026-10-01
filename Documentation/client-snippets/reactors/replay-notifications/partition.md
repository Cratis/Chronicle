```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

[EventType]
public record PartitionNotifiedOrderPlaced(string OrderId);

public class PartitionNotifiedOrderReactor : IReactor, ICanBeNotifiedWhenPartitionReplayed
{
    public Task BeginReplayPartition(Partition partition)
    {
        // The partition is the event source id being replayed.
        return Task.CompletedTask;
    }

    public Task EndReplayPartition(Partition partition)
    {
        return Task.CompletedTask;
    }

    public Task OrderPlaced(PartitionNotifiedOrderPlaced @event)
    {
        return Task.CompletedTask;
    }
}
```
