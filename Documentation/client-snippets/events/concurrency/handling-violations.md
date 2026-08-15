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
            SequenceNumber: EventSequenceNumber.BeforeFirst, // Expect no event for this account yet
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

        // False would mean nothing was compared against the event store
        return result.IsSuccess && result.ConcurrencyCheckPerformed;
    }
}
```
