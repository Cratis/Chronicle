```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record TaggingReactorsOrderShipped(string PhoneNumber, string TrackingNumber);

public interface ITaggingReactorsSmsService
{
    Task SendShippingNotification(string phoneNumber, string trackingNumber);
}

[Tag("Notifications", "SMS")]
[Tag("Customer")]
public class TaggingReactorsSmsNotificationReactor(ITaggingReactorsSmsService smsService) : IReactor
{
    public Task Shipped(TaggingReactorsOrderShipped @event, EventContext context) =>
        smsService.SendShippingNotification(@event.PhoneNumber, @event.TrackingNumber);
}
```
