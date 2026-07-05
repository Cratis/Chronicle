```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class WaitlistNotifierSideEffect(INotificationService notifications) : IReactor
{
    public async Task<WaitlistNotificationSent> BookReturned(BookReturned @event, EventContext context)
    {
        await notifications.NotifyNextInLine(context.EventSourceId);
        return new WaitlistNotificationSent();
    }
}
```
