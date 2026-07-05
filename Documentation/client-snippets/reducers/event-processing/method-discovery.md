```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record EventProcessingOrderCreated(Guid OrderId);

[EventType]
public record EventProcessingItemAdded(decimal Price);

public record EventProcessingOrderSummary(Guid OrderId, decimal Total, DateTimeOffset LastUpdated);

public class EventProcessingOrderSummaryReducer : IReducerFor<EventProcessingOrderSummary>
{
    public EventProcessingOrderSummary Created(EventProcessingOrderCreated @event, EventProcessingOrderSummary? current, EventContext context)
    {
        return new EventProcessingOrderSummary(@event.OrderId, 0m, context.Occurred);
    }

    public EventProcessingOrderSummary? ItemAdded(EventProcessingItemAdded @event, EventProcessingOrderSummary? current, EventContext context)
    {
        if (current is null) return null; // Skip if no order exists

        return current with
        {
            Total = current.Total + @event.Price,
            LastUpdated = context.Occurred
        };
    }
}
```
