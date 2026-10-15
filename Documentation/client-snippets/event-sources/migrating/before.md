```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record PaymentReceived(decimal Amount);

public class PaymentWriter(IEventLog eventLog)
{
    // The routing values are strings that every caller must repeat exactly.
    public Task<AppendResult> Receive(EventSourceId orderId, decimal amount, string period) =>
        eventLog.Append(
            orderId,
            new PaymentReceived(amount),
            eventStreamType: "payments",
            eventStreamId: period,
            eventSourceType: "order");
}

[EventSourceType("order")]
[EventStreamType("payments")]
public class PaymentReactor : IReactor
{
    public Task Received(PaymentReceived @event, EventContext context) => Task.CompletedTask;
}
```
