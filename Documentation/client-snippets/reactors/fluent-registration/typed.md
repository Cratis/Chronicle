```csharp
using Cratis.Chronicle.Events;

[EventType]
public record InvoiceIssued(string Customer, decimal Amount);

[EventType]
public record InvoicePaid(decimal Amount);

public interface IInvoiceNotifications
{
    Task Issued(string customer, decimal amount);
    Task Paid(EventSourceId invoice, decimal amount);
}

public class InvoiceReactorRegistration
{
    public async Task Register(IEventStore eventStore, IInvoiceNotifications notifications)
    {
        await eventStore.Reactors.Register(
            "invoice-notifications",
            reactor => reactor
                .On<InvoiceIssued>(@event => notifications.Issued(@event.Customer, @event.Amount))
                .On<InvoicePaid>((@event, context) => notifications.Paid(context.EventSourceId, @event.Amount)));
    }
}
```
