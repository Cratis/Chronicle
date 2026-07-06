```csharp
using Cratis.Chronicle.Events;

[EventType]
public record TaggedOrderPlaced(string CustomerId, decimal Total);

public class TaggedCheckoutService(IEventLog eventLog)
{
    public Task<AppendResult> PlaceOrder(OrderId orderId, string customerId, decimal total)
    {
        return eventLog.Append(
            orderId,
            new TaggedOrderPlaced(customerId, total),
            tags: ["checkout", "priority"]);
    }
}
```
