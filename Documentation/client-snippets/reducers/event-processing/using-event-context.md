```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record EventProcessingContextOrderPlaced(Guid OrderId, decimal Amount);

public record EventProcessingOrderSummaryWithContext(
    Guid OrderId,
    decimal Total,
    DateTimeOffset PlacedAt,
    string PlacedBy,
    CorrelationId CorrelationId);

public class EventProcessingOrderSummaryWithContextReducer : IReducerFor<EventProcessingOrderSummaryWithContext>
{
    public EventProcessingOrderSummaryWithContext Placed(EventProcessingContextOrderPlaced @event, EventProcessingOrderSummaryWithContext? current, EventContext context) =>
        new(
            OrderId: @event.OrderId,
            Total: @event.Amount,
            PlacedAt: context.Occurred,
            PlacedBy: context.CausedBy.ToString()!,
            CorrelationId: context.CorrelationId);
}
```
