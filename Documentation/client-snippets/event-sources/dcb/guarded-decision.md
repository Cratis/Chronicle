```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

[EventType]
public record MoneyWithdrawn(decimal Amount);

[EventSource("Account", Concurrency = ConcurrencyDimensions.EventSourceId)]
[EventStream(
    "Transactions",
    Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)]
public class AccountEventSource : IEventSource;

// The decision model: how many withdrawals this account has made in one month's stream.
[Passive]
[FromEvent<MoneyWithdrawn>]
public record MonthlyWithdrawals(string Id, [Count<MoneyWithdrawn>] int Count);

public enum WithdrawOutcome
{
    Withdrawn,
    LimitReached,
    ChangedConcurrently,
    Failed
}

public static class Withdraw
{
    const int MaxWithdrawalsPerMonth = 5;

    public static async Task<WithdrawOutcome> Handle(IEventStore eventStore, EventSourceId accountId, string month, decimal amount)
    {
        var definition = eventStore.EventSources.GetFor(typeof(AccountEventSource));
        var transactions = definition.FindStream("Transactions")!;

        // The read notes the log position before it folds, and folds only this month's stream.
        var read = await eventStore.GetDecisionReads().GetDetached<MonthlyWithdrawals>(
            accountId,
            transactions.EventStreamType,
            month,
            definition.EventSourceType);

        if ((read.Instance?.Count ?? 0) >= MaxWithdrawalsPerMonth)
        {
            return WithdrawOutcome.LimitReached;
        }

        // The event goes through the definition. The guard is the append condition.
        var result = await eventStore.EventLog.AppendMany(
            [
                new EventForEventSourceId(accountId, new MoneyWithdrawn(amount))
                {
                    EventSource = typeof(AccountEventSource),
                    EventStream = "Transactions",
                    EventStreamId = month
                }
            ],
            guardedBy: [read]);

        if (result.GetDecisionConflicts([read]).Any())
        {
            return WithdrawOutcome.ChangedConcurrently;
        }

        return result.IsSuccess ? WithdrawOutcome.Withdrawn : WithdrawOutcome.Failed;
    }
}
```
