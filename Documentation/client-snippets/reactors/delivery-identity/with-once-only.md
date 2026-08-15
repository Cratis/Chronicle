```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record CustomerWelcomed(string CustomerId);

public interface IWelcomeMail
{
    Task Send(string customerId);
}

public class WelcomeMailer(IWelcomeMail mail, IDeliveryReceipts receipts) : IReactor
{
    [OnceOnly]
    public async Task SendWelcomeMail(CustomerWelcomed @event, ReactorDelivery delivery)
    {
        if (await receipts.HasCompleted(delivery.Id))
        {
            return;
        }

        await mail.Send(@event.CustomerId);
        await receipts.Complete(delivery.Id);
    }
}
```
