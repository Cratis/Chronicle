```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

[EventType]
public record StreamOrderPlaced(string Status);

[Passive]
[FromEvent<StreamOrderPlaced>]
public record StreamOrderEligibility(string Id, string Status);

public static class StreamDecisionExample
{
    public static async Task<AppendManyResult> PlaceOrder(
        IEventStore eventStore, EventSourceId orderId)
    {
        var read = await eventStore.GetDecisionReads().GetDetached<StreamOrderEligibility>(
            orderId, eventStreamType: "orders", eventStreamId: "checkout");
        // Make the decision using read.Instance, including the absent case.
        return await eventStore.EventLog.AppendMany(
            [new EventForEventSourceId(orderId, new StreamOrderPlaced("placed"))
            {
                EventStreamType = "orders",
                EventStreamId = "checkout"
            }],
            guardedBy: [read]);
        // The caller checks IsSuccess and handles conflicts before resubmitting.
    }
}
```
