```csharp
namespace PerStreamScenario
{
    using Cratis.Chronicle;
    using Cratis.Chronicle.Events;
    using Cratis.Chronicle.EventSequences;
    using Cratis.Chronicle.EventSources;
    using Cratis.Chronicle.Projections.ModelBound;
    using Cratis.Chronicle.ReadModels;

    public record SavingsAccountId(string Value) : EventSourceId(Value);

    [EventType]
    public record SavingsWithdrawn(decimal Amount);

    [EventSource("SavingsAccount")]
    [Cratis.Chronicle.EventSources.EventStream("Transactions")]
    public class SavingsAccountEventSource : IEventSource;

    // Passive, keyed by the account, and folded from the Transactions stream only.
    [EventStream<SavingsAccountEventSource>("Transactions")]
    public record WithdrawnInMonth(
        SavingsAccountId Id,
        [AddFrom<SavingsWithdrawn>(nameof(SavingsWithdrawn.Amount))] decimal Total);

    public static class WithdrawSavings
    {
        const decimal MonthlyLimit = 5_000m;

        public static async Task<bool> Handle(
            IEventStore eventStore, SavingsAccountId account, EventStreamId month, decimal amount)
        {
            var definition = eventStore.EventSources.GetFor(typeof(SavingsAccountEventSource));
            var read = await eventStore.GetDecisionReads().GetDetached<WithdrawnInMonth>(
                account, "Transactions", month, definition.EventSourceType);

            if ((read.Instance?.Total ?? 0m) + amount > MonthlyLimit)
            {
                return false;
            }

            var result = await eventStore.EventLog.AppendMany(
                [new EventForEventSourceId(account, new SavingsWithdrawn(amount))
                {
                    EventSourceType = definition.EventSourceType,
                    EventStreamType = "Transactions",
                    EventStreamId = month
                }],
                guardedBy: [read]);

            return result.IsSuccess;
        }
    }
}
```
