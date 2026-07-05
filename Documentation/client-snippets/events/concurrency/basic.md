```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

[EventType]
public record ConcurrencyAccountOpened(string AccountName);

public class ConcurrencyBankAccountService(IEventLog eventLog)
{
    public async Task OpenAccount(EventSourceId accountId, string accountName)
    {
        var concurrencyScope = new ConcurrencyScope(
            SequenceNumber: 42,
            EventSourceId: accountId);

        await eventLog.Append(
            accountId,
            new ConcurrencyAccountOpened(accountName),
            concurrencyScope: concurrencyScope);
    }
}
```
