```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record IdempotentPaymentDue(string OrderId, decimal Amount);

public interface IIdempotentPaymentGateway
{
    Task Charge(string orderId, decimal amount);
}

// Your storage, not Chronicle's. One row per completed delivery, keyed by its identity.
public interface IDeliveryReceipts
{
    Task<bool> HasCompleted(DeliveryId delivery);

    Task Complete(DeliveryId delivery);
}

public class IdempotentBilling(IIdempotentPaymentGateway payments, IDeliveryReceipts receipts) : IReactor
{
    public async Task PaymentDue(IdempotentPaymentDue @event, ReactorDelivery delivery)
    {
        if (await receipts.HasCompleted(delivery.Id))
        {
            return;
        }

        await payments.Charge(@event.OrderId, @event.Amount);
        await receipts.Complete(delivery.Id);
    }
}
```
