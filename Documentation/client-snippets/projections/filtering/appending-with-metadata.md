```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public class FilteringAppendService(IEventLog eventLog)
{
    public async Task AppendOrders(string customerId)
    {
        // Appends to all observers — no extra metadata
        await eventLog.Append(EventSourceId.New(), new FilteringWithReactorOrderPlaced(customerId, 42m));

        // Appends to all observers; additionally dispatched to observers filtering on "premium"
        await eventLog.Append(EventSourceId.New(), new FilteringWithReactorOrderPlaced(customerId, 299m), tags: ["premium"]);

        // Appends with stream type; dispatched to observers filtering on "wholesale" stream type
        await eventLog.Append(EventSourceId.New(), new FilteringWithReactorOrderPlaced(customerId, 1500m), eventStreamType: "wholesale");
    }
}
```
