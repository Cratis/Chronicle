```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

[EventType]
public record ConcurrencySafeAccountOpened(string AccountName);

public class ConcurrencySafeAccountService(IEventLog eventLog)
{
    public async Task<bool> TryOpenAccount(EventSourceId accountId, string accountName)
    {
        var concurrencyScope = new ConcurrencyScope(
            SequenceNumber: 0, // Expect this to be the first event
            EventSourceId: accountId);

        var result = await eventLog.Append(
            accountId,
            new ConcurrencySafeAccountOpened(accountName),
            concurrencyScope: concurrencyScope);

        if (result.HasConcurrencyViolations)
        {
            // result.ConcurrencyViolation describes the expected vs actual sequence number
            return false;
        }

        return result.IsSuccess;
    }
}
```
