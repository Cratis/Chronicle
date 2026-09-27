```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

var read = await eventStore.GetDecisionReads().GetDetached<OrderEligibility>(orderId);
if (!read.Exists)
{
    // Decide whether creation is allowed, including the absent case.
}

var result = await eventStore.EventLog.AppendMany(
    [new EventForEventSourceId(orderId, new OrderPlaced())],
    guardedBy: [read]);
var conflicts = result.GetDecisionConflicts([read]);
if (conflicts.Any())
{
    // The decision must be read again and resubmitted; no sequence numbers are exposed.
}
// Check result.IsSuccess separately for constraint violations and other append failures.
```
