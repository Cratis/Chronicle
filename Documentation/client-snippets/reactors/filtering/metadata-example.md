```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
public record ReactorsFilteringOrderPlaced(decimal TotalAmount);

public class ReactorsFilteringMetadataExampleService(IEventLog eventLog)
{
    public Task PlaceOrder(decimal totalAmount) =>
        eventLog.Append(
            EventSourceId.New(),
            new ReactorsFilteringOrderPlaced(totalAmount),
            tags: ["priority"],
            eventSourceType: "order",
            eventStreamType: "fulfillment");
}
```
