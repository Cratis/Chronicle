```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

[EventType]
public record ObservingAppendsFirstEvent(string Data);

[EventType]
public record ObservingAppendsSecondEvent(string Data);

public class ObservingAppendsBatchCompletionWaiter(IEventLog eventLog)
{
    public async Task AppendManyAndWait(EventSourceId eventSourceId)
    {
        var appendManyResult = await eventLog.AppendMany(eventSourceId, new object[]
        {
            new ObservingAppendsFirstEvent("first"),
            new ObservingAppendsSecondEvent("second")
        });

        var completion = await appendManyResult.WaitForCompletion();
        if (!completion.IsSuccess)
        {
            // Inspect failed partitions from affected observers
        }
    }
}
```
