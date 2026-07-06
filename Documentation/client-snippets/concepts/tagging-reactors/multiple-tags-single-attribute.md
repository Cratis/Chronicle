```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record TaggingReactorsCustomerRegistered(string Email, string Name);

public interface ITaggingReactorsWelcomeEmailService
{
    Task SendWelcomeEmail(string email, string name);
}

[Tag("Notifications", "Customer", "Email")]
public class TaggingReactorsCustomerNotificationReactor(ITaggingReactorsWelcomeEmailService emailService) : IReactor
{
    public Task Registered(TaggingReactorsCustomerRegistered @event, EventContext context) =>
        emailService.SendWelcomeEmail(@event.Email, @event.Name);
}
```
