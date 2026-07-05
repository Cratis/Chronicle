```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record EventProcessingSkipItemAdded(decimal Price);

public record EventProcessingSkipOrderSummary(decimal Total);

public class EventProcessingSkipOrderSummaryReducer : IReducerFor<EventProcessingSkipOrderSummary>
{
    public EventProcessingSkipOrderSummary? ItemAdded(EventProcessingSkipItemAdded @event, EventProcessingSkipOrderSummary? current, EventContext context)
    {
        // Can't add items if order doesn't exist
        if (current is null) return null;

        return current with
        {
            Total = current.Total + @event.Price
        };
    }
}
```
