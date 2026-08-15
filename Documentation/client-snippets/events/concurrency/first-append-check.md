```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

public static class ConcurrencyFirstAppendCheckSetup
{
    // Application-wide: every first append into a scope the optimistic strategy resolves is checked.
    public static void ConfigureChronicle(ChronicleOptions options) =>
        options.ConcurrencyOptions.CheckFirstAppendIntoAScope = true;
}

[EventType]
public record ConcurrencyFirstAppendPartitionOpened(string Name);

public class ConcurrencyFirstAppendPartitionService(IEventLog eventLog)
{
    // Per append: ask for the same check on one behavior, without turning it on everywhere.
    public async Task<bool> TryOpenPartition(EventSourceId accountId, string name)
    {
        var concurrencyScope = new ConcurrencyScopeBuilder()
            .ExpectingNoMatchingEvent()
            .WithEventSourceId(accountId)
            .WithEventType<ConcurrencyFirstAppendPartitionOpened>()
            .Build();

        var result = await eventLog.Append(
            accountId,
            new ConcurrencyFirstAppendPartitionOpened(name),
            concurrencyScope: concurrencyScope);

        return result.IsSuccess;
    }
}
```
