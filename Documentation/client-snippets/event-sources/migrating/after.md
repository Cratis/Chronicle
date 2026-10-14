```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reactors;

// The names are the strings that are already stored on existing events, so those events still match.
[EventSource("order")]
[EventStream("payments")]
public class OrderEventSource : IEventSource;

public class TypedPaymentWriter(IEventLog eventLog)
{
    public Task<AppendResult> Receive(EventSourceId orderId, decimal amount, string period) =>
        eventLog.Append<OrderEventSource>(
            orderId,
            new PaymentReceived(amount),
            eventStream: "payments",
            eventStreamId: period);
}

[FromEventSource<OrderEventSource>("payments")]
public class TypedPaymentReactor : IReactor
{
    public Task Received(PaymentReceived @event, EventContext context) => Task.CompletedTask;
}
```
