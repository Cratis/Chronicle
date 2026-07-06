```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record ReactorsFilteringMultiTagOrderPlaced(decimal TotalAmount);

[FilterEventsByTag("priority")]
[FilterEventsByTag("express")]
public class ReactorsFilteringFastTrackOrderNotifier : IReactor
{
    public Task Placed(ReactorsFilteringMultiTagOrderPlaced @event, EventContext context) =>
        Task.CompletedTask;
}
```
