```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

[EventType]
public record ConcurrencyAccountNameChanged(string NewName);

// Configured automatically when using dependency injection
public class ConcurrencyOptimisticAccountService(IEventLog eventLog, IConcurrencyScopeStrategies strategies)
{
    public async Task UpdateAccount(EventSourceId accountId, string newName)
    {
        var strategy = strategies.GetFor(eventLog);
        var concurrencyScope = await strategy.GetScope(accountId);

        await eventLog.Append(
            accountId,
            new ConcurrencyAccountNameChanged(newName),
            concurrencyScope: concurrencyScope);
    }
}
```
