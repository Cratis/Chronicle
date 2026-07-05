```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public interface INotificationService
{
    Task NotifyNextInLine(EventSourceId bookId);
    Task NotifyNextInLine(EventSourceId bookId, string bookTitle);
}

public class WaitlistNotifier(INotificationService notifications) : IReactor
{
    public async Task BookReturned(BookReturned @event, EventContext context)
    {
        // context.EventSourceId is the BookId this happened to
        await notifications.NotifyNextInLine(context.EventSourceId);
    }
}
```
