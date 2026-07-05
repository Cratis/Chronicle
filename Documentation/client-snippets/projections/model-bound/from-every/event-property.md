```csharp title="Read a shared event property from every event"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public enum OrderStateFromEvery
{
    New,
    Confirmed,
    Shipped
}

[EventType]
public record OrderConfirmedForEvery(OrderStateFromEvery Status);

[EventType]
public record OrderShippedForEvery(OrderStateFromEvery Status);

[FromEvent<OrderConfirmedForEvery>]
[FromEvent<OrderShippedForEvery>]
public record OrderStatusFromEvery(
    [Key] Guid Id,
    [FromEvery(property: nameof(OrderConfirmedForEvery.Status))]
    OrderStateFromEvery CurrentStatus);
```
