```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record ReactorsFilteringPaymentCaptured(decimal Amount);

public class ReactorsFilteringPaymentsService(IEventLog eventLog)
{
    public Task Capture(decimal amount) =>
        eventLog.Append(
            EventSourceId.New(),
            new ReactorsFilteringPaymentCaptured(amount),
            eventStreamType: "payments");
}

[EventStreamType("payments")]
public class ReactorsFilteringPaymentReceivedNotifier : IReactor
{
    public Task Captured(ReactorsFilteringPaymentCaptured @event, EventContext context) =>
        Task.CompletedTask;
}
```
