```csharp
using Cratis.Chronicle.Events;

[EventType]
public record OrderPlaced(string CustomerId, decimal Total);

public class CheckoutService(IEventLog eventLog)
{
    public async Task PlaceOrder(OrderId orderId, string customerId, decimal total)
    {
        var result = await eventLog.Append(
            orderId,
            new OrderPlaced(customerId, total)
        );

        if (!result.IsSuccess)
        {
            // Decide whether to retry or surface a conflict to the caller.
        }
    }
}
```
