```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

public record BankAccountId(string Value) : EventSourceId(Value);

[EventType]
public record MoneyWithdrawn(decimal Amount);

// Names the source type and the stream, so the read and the append use the same values.
[EventSource("BankAccount")]
[EventStream("Transactions", Description = "Deposits and withdrawals, one stream per month")]
public class BankAccountEventSource : IEventSource;

// The decision model: what this account has withdrawn in the stream the read is scoped to.
[Passive]
[FromEvent<MoneyWithdrawn>]
public record WithdrawnThisMonth(string Id, [AddFrom<MoneyWithdrawn>(nameof(MoneyWithdrawn.Amount))] decimal Total);

public enum WithdrawOutcome
{
    Withdrawn,
    MonthlyLimitReached,
    ChangedConcurrently,
    Failed
}

public static class Withdraw
{
    const decimal MonthlyLimit = 5000m;

    // month is the stream ID, for example "2026-10".
    public static async Task<WithdrawOutcome> Handle(IEventStore eventStore, BankAccountId account, string month, decimal amount)
    {
        var definition = eventStore.EventSources.GetFor(typeof(BankAccountEventSource));
        var transactions = definition.FindStream("Transactions")!;

        // Reads only BankAccount / Transactions / month, and guards only that stream.
        var read = await eventStore.GetDecisionReads().GetDetached<WithdrawnThisMonth>(
            account,
            transactions.EventStreamType,
            month,
            definition.EventSourceType);

        if ((read.Instance?.Total ?? 0m) + amount > MonthlyLimit)
        {
            return WithdrawOutcome.MonthlyLimitReached;
        }

        var result = await eventStore.EventLog.AppendMany(
            [
                new EventForEventSourceId(account, new MoneyWithdrawn(amount))
                {
                    EventSource = typeof(BankAccountEventSource),
                    EventStream = "Transactions",
                    EventStreamId = month
                }
            ],
            guardedBy: [read]);

        if (result.GetDecisionConflicts([read]).Any())
        {
            // A withdrawal in this month's stream was appended after the read. Nothing was appended.
            return WithdrawOutcome.ChangedConcurrently;
        }

        return result.IsSuccess ? WithdrawOutcome.Withdrawn : WithdrawOutcome.Failed;
    }
}
```
