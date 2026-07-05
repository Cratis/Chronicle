```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record ScenariosReactBookReturned(string Isbn);

public interface IScenariosReactNotificationService
{
    Task NotifyNextInLine(EventSourceId bookId);
    Task NotifyNextInLine(EventSourceId bookId, string bookTitle);
}

public class ScenariosReactWaitlistNotifier(IScenariosReactNotificationService notifications) : IReactor
{
    public async Task BookReturned(ScenariosReactBookReturned @event, EventContext context)
    {
        // context.EventSourceId is the source the event happened to (the book)
        await notifications.NotifyNextInLine(context.EventSourceId);
    }
}
```
