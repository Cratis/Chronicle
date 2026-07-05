```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class NotificationWasNotRecorded(EventSourceId bookId) : Exception($"Notification for book {bookId} was not recorded");

public class WaitlistNotifierExplicitAppend(IEventStore eventStore, INotificationService notifications) : IReactor
{
    public async Task BookReturned(BookReturned @event, EventContext context)
    {
        await notifications.NotifyNextInLine(context.EventSourceId);

        var result = await eventStore.EventLog.Append(context.EventSourceId, new WaitlistNotificationSent());
        if (!result.IsSuccess) throw new NotificationWasNotRecorded(context.EventSourceId);
    }
}
```
