```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

[EventType]
public record ConcurrencyAccountSettingsUpdated(string Settings);

public class ConcurrencyAccountManagementService(IEventLog eventLog)
{
    public async Task UpdateAccountSettings(EventSourceId accountId, string settings)
    {
        var concurrencyScope = new ConcurrencyScopeBuilder()
            .WithEventSourceId(accountId)
            .WithEventSourceType("BankAccount")
            .WithEventStreamType("AccountManagement")
            .WithSequenceNumber(10)
            .Build();

        await eventLog.Append(
            accountId,
            new ConcurrencyAccountSettingsUpdated(settings),
            eventSourceType: "BankAccount",
            eventStreamType: "AccountManagement",
            concurrencyScope: concurrencyScope);
    }
}
```
