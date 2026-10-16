```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

// Uses the Account definition and the MoneyWithdrawn event from the first scenario.
// The decision model totals the withdrawals in the stream it is read for.
[Passive]
[FromEvent<MoneyWithdrawn>]
public record WithdrawalsThisMonth(string Id, [AddFrom<MoneyWithdrawn>(nameof(MoneyWithdrawn.Amount))] decimal Total);

public class LimitedWithdrawals(IEventStore eventStore)
{
    const decimal MonthlyLimit = 5000m;

    public async Task<bool> Withdraw(EventSourceId accountId, string month, decimal amount)
    {
        var definition = eventStore.EventSources.GetFor(typeof(AccountEventSource));
        var transactions = definition.FindStream("Transactions")!;

        // The read and the append take the same names from the definition.
        var read = await eventStore.GetDecisionReads().GetDetached<WithdrawalsThisMonth>(
            accountId,
            transactions.EventStreamType,
            month,
            definition.EventSourceType);

        if ((read.Instance?.Total ?? 0m) + amount > MonthlyLimit)
        {
            return false;
        }

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

        return result.IsSuccess;
    }
}
```
