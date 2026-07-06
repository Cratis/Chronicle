```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class WaitlistNotifierWithBookTitle(IEventStore eventStore, INotificationService notifications) : IReactor
{
    public async Task BookReturned(BookReturned @event, EventContext context)
    {
        var book = await eventStore.ReadModels.GetInstanceById<Book>(context.EventSourceId);
        await notifications.NotifyNextInLine(context.EventSourceId, book.Title);
    }
}
```
