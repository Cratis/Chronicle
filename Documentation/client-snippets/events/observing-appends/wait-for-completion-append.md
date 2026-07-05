```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

[EventType]
public record ObservingAppendsSomeEvent(string Data);

public class ObservingAppendsCompletionWaiter(IEventLog eventLog)
{
    public async Task AppendAndWait(EventSourceId eventSourceId)
    {
        var appendResult = await eventLog.Append(eventSourceId, new ObservingAppendsSomeEvent("example"));
        var completion = await appendResult.WaitForCompletion();

        if (!completion.IsSuccess)
        {
            foreach (var failedPartition in completion.FailedPartitions)
            {
                Console.WriteLine($"Observer {failedPartition.ObserverId} failed partition {failedPartition.Partition}");
            }
        }
    }
}
```
