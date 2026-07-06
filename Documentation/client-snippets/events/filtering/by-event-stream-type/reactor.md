```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record FilterByStreamTypePaymentCaptured(decimal Amount);

public class FilterByStreamTypePaymentsService(IEventLog eventLog)
{
    public Task Capture(decimal amount) =>
        eventLog.Append(
            EventSourceId.New(),
            new FilterByStreamTypePaymentCaptured(amount),
            eventStreamType: "payments");
}

[EventStreamType("payments")]
public class FilterByStreamTypePaymentNotificationsReactor : IReactor
{
    public Task Captured(FilterByStreamTypePaymentCaptured @event, EventContext context) => Task.CompletedTask;
}
```
