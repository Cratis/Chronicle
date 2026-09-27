```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

public record DecisionReadsOrderPlaced(string OrderId);

public record DecisionReadsOrderEligibility(bool Eligible);

public class DecisionReadsGuardedAppend
{
    public async Task Run(IEventStore eventStore, string orderId)
    {
        var read = await eventStore.GetDecisionReads().GetDetached<DecisionReadsOrderEligibility>(orderId);
        if (!read.Exists)
        {
            // Decide whether creation is allowed, including the absent case.
        }

        var result = await eventStore.EventLog.AppendMany(
            [new EventForEventSourceId(orderId, new DecisionReadsOrderPlaced(orderId))],
            guardedBy: [read]);
        var conflicts = result.GetDecisionConflicts([read]);
        if (conflicts.Any())
        {
            // The decision must be read again and resubmitted; no sequence numbers are exposed.
        }
        // Check result.IsSuccess separately for constraint violations and other append failures.
    }
}
```
