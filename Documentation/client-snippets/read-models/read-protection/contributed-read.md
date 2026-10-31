```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

[EventType]
public record ProtectedDeposit(decimal Amount);

public static class ProtectedAccountHistory
{
    public static async Task<decimal> LoadBalance(IEventStore eventStore, IUnitOfWork unitOfWork, EventSourceId accountId)
    {
        var deposit = eventStore.EventTypes.GetEventTypeFor(typeof(ProtectedDeposit));
        using (unitOfWork.AllowReads(ReadAllowance.Contributed, "Load the account history"))
        {
            // Capture the boundary before reading, then enroll before the value is used.
            var boundary = await eventStore.EventLog.GetTailSequenceNumber();
            var events = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(accountId, [deposit]);
            unitOfWork.EnrolContributedRead(new ContributedRead(
                ConcurrencyScopeOrigin.Explicit,
                accountId,
                new ConcurrencyScope(boundary, accountId, EventTypes: [deposit]),
                eventStore.Name,
                eventStore.Namespace,
                EventSequenceId.Log,
                $"Account history for '{accountId}'"));
            return events.Sum(_ => ((ProtectedDeposit)_.Content).Amount);
        }
    }
}
```
