```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
public record NamedTaggedOrderPlaced(decimal Total);

public class NamedTaggedCheckoutService(IEventLog eventLog)
{
    public Task<AppendResult> PlaceOrder(EventSourceId orderId, string checkoutSessionId, decimal total) =>
        eventLog.Append(
            orderId,
            new NamedTaggedOrderPlaced(total),
            namedTags:
            [
                new NamedTag("checkout-session", checkoutSessionId),
                new NamedTag("sales-channel", "web")
            ],
            tags: ["checkout"]);
}
```
