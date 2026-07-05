```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
public record ReducersFilteringOrderPlaced(decimal TotalAmount);

public class ReducersFilteringMetadataExampleService(IEventLog eventLog)
{
    public Task PlaceOrder(decimal totalAmount) =>
        eventLog.Append(
            EventSourceId.New(),
            new ReducersFilteringOrderPlaced(totalAmount),
            tags: ["priority"],
            eventSourceType: "order",
            eventStreamType: "fulfillment");
}
```
