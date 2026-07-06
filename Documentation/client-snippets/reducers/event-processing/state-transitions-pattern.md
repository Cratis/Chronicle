```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record EventProcessingOrderCreatedForStatus(Guid OrderId);

[EventType]
public record EventProcessingOrderPaid(Guid OrderId);

[EventType]
public record EventProcessingOrderShipped(Guid OrderId);

[EventType]
public record EventProcessingOrderDelivered(Guid OrderId);

[EventType]
public record EventProcessingOrderCancelled(Guid OrderId);

public record EventProcessingOrderStatus(string State, DateTimeOffset LastUpdated);

public class EventProcessingOrderStatusReducer : IReducerFor<EventProcessingOrderStatus>
{
    public EventProcessingOrderStatus Created(EventProcessingOrderCreatedForStatus @event, EventProcessingOrderStatus? current, EventContext context)
        => new EventProcessingOrderStatus("Created", context.Occurred);

    public EventProcessingOrderStatus Paid(EventProcessingOrderPaid @event, EventProcessingOrderStatus? current, EventContext context)
        => new EventProcessingOrderStatus("Paid", context.Occurred);

    public EventProcessingOrderStatus Shipped(EventProcessingOrderShipped @event, EventProcessingOrderStatus? current, EventContext context)
        => new EventProcessingOrderStatus("Shipped", context.Occurred);

    public EventProcessingOrderStatus Delivered(EventProcessingOrderDelivered @event, EventProcessingOrderStatus? current, EventContext context)
        => new EventProcessingOrderStatus("Delivered", context.Occurred);

    public EventProcessingOrderStatus Cancelled(EventProcessingOrderCancelled @event, EventProcessingOrderStatus? current, EventContext context)
        => new EventProcessingOrderStatus("Cancelled", context.Occurred);
}
```
