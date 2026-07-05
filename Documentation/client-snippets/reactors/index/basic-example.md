```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record ReactorsIndexEmailConfirmed(string Email);

public class ReactorsIndexEmailNotificationsReactor : IReactor
{
    public Task Confirmed(ReactorsIndexEmailConfirmed @event, EventContext context) =>
        SendConfirmationAsync(@event.Email, context.Occurred);

    Task SendConfirmationAsync(string email, DateTimeOffset occurred) => Task.CompletedTask;
}
```
