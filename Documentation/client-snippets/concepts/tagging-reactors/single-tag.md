```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record TaggingReactorsOrderPlaced(string CustomerId, string OrderId);

public interface ITaggingReactorsEmailService
{
    Task SendOrderConfirmation(string customerId, string orderId);
}

[Tag("Notifications")]
public class TaggingReactorsOrderConfirmationReactor(ITaggingReactorsEmailService emailService) : IReactor
{
    public Task Placed(TaggingReactorsOrderPlaced @event, EventContext context) =>
        emailService.SendOrderConfirmation(@event.CustomerId, @event.OrderId);
}
```
