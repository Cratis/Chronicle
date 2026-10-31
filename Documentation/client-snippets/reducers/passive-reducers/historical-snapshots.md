```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

[EventType]
public record PassivePrefixAmountRecorded(decimal Amount);

[Passive]
public record PassiveReducersAccountBalance(decimal Balance)
{
    public static PassiveReducersAccountBalance Reduce(PassivePrefixAmountRecorded @event, PassiveReducersAccountBalance? current) =>
        new((current?.Balance ?? 0m) + @event.Amount);
}

public class PassiveReducersPrefixAnalysis(IEventStore eventStore)
{
    public Task<IEnumerable<PassiveReducersAccountBalance>> GetBalancesFromPrefix(EventCount eventCount) =>
        eventStore.ReadModels.GetInstances<PassiveReducersAccountBalance>(eventCount);
}
```
