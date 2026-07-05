```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class WaitlistNotifierOnceOnly(INotificationService notifications) : IReactor
{
    [OnceOnly]
    public async Task BookReturned(BookReturned @event, EventContext context) =>
        await notifications.NotifyNextInLine(context.EventSourceId);
}
```
