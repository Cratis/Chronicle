```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

[EventType]
public record GuardedOrderPlaced(string CustomerId);

[FromEvent<GuardedOrderPlaced>]
public record OrderEligibility([Key] string Id, string CustomerId);

public class OrderDecision(IEventStore eventStore)
{
    public async Task PlaceOrder(EventSourceId orderId, string customerId)
    {
        var read = await eventStore.GetDecisionReads().GetDetached<OrderEligibility>(orderId);
        if (!read.Exists)
        {
            // Decide whether creation is allowed, including the absent case.
        }

        var result = await eventStore.EventLog.AppendMany(
            [new EventForEventSourceId(orderId, new GuardedOrderPlaced(customerId))],
            guardedBy: [read]);
        var conflicts = result.GetDecisionConflicts([read]);
        if (conflicts.Any())
        {
            throw new InvalidOperationException("Re-read and retry the decision in a new request.");
        }
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException("The guarded append failed; inspect constraint violations and other failures.");
        }
    }
}
```
