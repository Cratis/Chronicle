```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

[EventType]
public record OrderPlaced(string Customer, [property: Unique] string IdempotencyToken);

public enum PlaceOrderOutcome
{
    Placed,
    AlreadyPlaced,
    Failed
}

public static class PlaceOrder
{
    // The caller generates the token once per order and sends the same token on every retry.
    // The order's event source id is generated here, so a retry is a different source with the same token.
    public static async Task<PlaceOrderOutcome> Handle(IEventLog eventLog, string customer, string idempotencyToken)
    {
        var result = await eventLog.Append(EventSourceId.New(), new OrderPlaced(customer, idempotencyToken));

        if (result.HasConstraintViolations)
        {
            return PlaceOrderOutcome.AlreadyPlaced;
        }

        return result.IsSuccess ? PlaceOrderOutcome.Placed : PlaceOrderOutcome.Failed;
    }
}
```
