```csharp
namespace MonthlyLimitExample
{
    using Cratis.Chronicle;
    using Cratis.Chronicle.Events;
    using Cratis.Chronicle.EventSequences;
    using Cratis.Chronicle.EventSources;
    using Cratis.Chronicle.Projections.ModelBound;
    using Cratis.Chronicle.ReadModels;

    public record AccountId(string Value) : EventSourceId(Value);

    [EventSource("BankAccount")]
    [Cratis.Chronicle.EventSources.EventStream("Transactions")]
    public class BankAccountEventSource : IEventSource;

    [EventType]
    public record MoneyWithdrawn(decimal Amount);

    [EventStream<BankAccountEventSource>("Transactions")]
    public record MonthlyWithdrawals(
        AccountId Id,
        [AddFrom<MoneyWithdrawn>(nameof(MoneyWithdrawn.Amount))] decimal Withdrawn);

    public record WithdrawalOutcome(bool LimitReached, AppendManyResult? Append);

    public static class OctoberWithdrawals
    {
        public static async Task<WithdrawalOutcome> Withdraw(
            IEventStore eventStore, AccountId account, decimal amount)
        {
            var definition = eventStore.EventSources.GetFor(typeof(BankAccountEventSource));
            EventStreamId month = "2026-10";
            var read = await eventStore.GetDecisionReads().GetDetached<MonthlyWithdrawals>(
                account, "Transactions", month, definition.EventSourceType);
            if ((read.Instance?.Withdrawn ?? 0m) + amount > 10_000m)
            {
                return new(true, null);
            }

            var result = await eventStore.EventLog.AppendMany(
                [new EventForEventSourceId(account, new MoneyWithdrawn(amount))
                {
                    EventSourceType = definition.EventSourceType,
                    EventStreamType = "Transactions",
                    EventStreamId = month
                }],
                guardedBy: [read]);
            return new(false, result);
        }

        public static Task<MonthlyWithdrawals> ForAccount(
            IEventStore eventStore, AccountId account) =>
            eventStore.ReadModels.GetInstanceById<MonthlyWithdrawals>(account, "2026-10");
    }
}
```
