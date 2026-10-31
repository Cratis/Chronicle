```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

public record AccountId(string Value) : EventSourceId(Value);

[EventType]
public record MoneyWithdrawn(decimal Amount);

// Counts the withdrawals in the stream the read is scoped to.
[Passive]
[FromEvent<MoneyWithdrawn>]
public record MonthlyWithdrawals(string Id, [Count<MoneyWithdrawn>] int Count);

public static class ShowTheGuard
{
    public static async Task<IReadOnlyList<AppendCondition>> Withdraw(IEventStore eventStore, AccountId account)
    {
        var unitOfWork = (UnitOfWork)eventStore.UnitOfWorkManager.Begin(CorrelationId.New());
        using var frame = unitOfWork.EnforceReadProtection();
        var owner = unitOfWork.ClaimDecisionReadCommitOwnership();

        // Account, stream type "Transactions", stream id "2026-10".
        await eventStore.GetDecisionReads().GetFor<MonthlyWithdrawals>(unitOfWork, account, "Transactions", "2026-10");
        unitOfWork.AddEvent(
            EventSequenceId.Log,
            account,
            new MoneyWithdrawn(100),
            Causation.Unknown(),
            eventStreamType: "Transactions",
            eventStreamId: "2026-10");
        await unitOfWork.CommitAsOwner(owner);

        // One AppendCondition per guarded event source. Print it to see what the commit guarded.
        return unitOfWork.GetAppendConditions();
    }
}
```
