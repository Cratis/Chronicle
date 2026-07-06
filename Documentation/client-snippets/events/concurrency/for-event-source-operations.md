```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Operations;

[EventType]
public record ConcurrencyAccountValidated;

[EventType]
public record ConcurrencyAccountProcessed;

public class ConcurrencyBatchAccountProcessor(IEventLog eventLog)
{
    public async Task ProcessAccountBatch(EventSourceId accountId)
    {
        await eventLog
            .ForEventSourceId(accountId, source => source
                .WithConcurrencyScope(scope => scope
                    .WithSequenceNumber(30)
                    .WithEventType<ConcurrencyAccountProcessed>()
                    .WithEventType<ConcurrencyAccountValidated>())
                .Append(new ConcurrencyAccountValidated())
                .Append(new ConcurrencyAccountProcessed()))
            .Perform();
    }
}
```
