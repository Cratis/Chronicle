```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public record ScenariosReactBook(string Title);

public class ScenariosReactWaitlistNotifierWithTitle(IEventStore eventStore, IScenariosReactNotificationService notifications) : IReactor
{
    public async Task BookReturned(ScenariosReactBookReturned @event, EventContext context)
    {
        // Strongly consistent — rebuilt from the event log, includes this event
        var book = await eventStore.ReadModels.GetInstanceById<ScenariosReactBook>(context.EventSourceId);
        await notifications.NotifyNextInLine(context.EventSourceId, book.Title);
    }
}
```
