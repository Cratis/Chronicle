```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

public static class TransactionalTransferWorkflow
{
    public static async Task<bool> TryCommitTransfer(
        IEventStore store,
        EventSequenceNumber expectedAuthorizationRevision)
    {
        using var unitOfWork = store.UnitOfWorkManager.Begin(CorrelationId.New());
        EventSourceId authorizationScopeLabel = "authorization-history";
        var authorizationScope = new ConcurrencyScope(
            expectedAuthorizationRevision,
            EventStreamType: "authorization");

        unitOfWork.AddEvents(
            EventSequenceId.Log,
            [
                new EventForEventSourceId(
                    "account-from",
                    new TransferDebited(100m)),
                new EventForEventSourceId(
                    "account-to",
                    new TransferCredited(100m))
            ],
            [new(authorizationScopeLabel, authorizationScope)]);

        await unitOfWork.Commit();
        return unitOfWork.IsSuccess;
    }
}

[EventType]
public record TransferDebited(decimal Amount);

[EventType]
public record TransferCredited(decimal Amount);
```
