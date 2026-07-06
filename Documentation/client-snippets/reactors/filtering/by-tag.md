```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record ReactorsFilteringByTagOrderPlaced(decimal TotalAmount);

public class ReactorsFilteringByTagOrderService(IEventLog eventLog)
{
    public Task PlacePriorityOrder(decimal totalAmount) =>
        eventLog.Append(
            EventSourceId.New(),
            new ReactorsFilteringByTagOrderPlaced(totalAmount),
            tags: ["priority"]);
}

[FilterEventsByTag("priority")]
public class ReactorsFilteringPriorityOrderNotifier : IReactor
{
    public Task Placed(ReactorsFilteringByTagOrderPlaced @event, EventContext context) =>
        Task.CompletedTask;
}
```
