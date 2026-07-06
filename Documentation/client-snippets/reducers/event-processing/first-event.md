```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record EventProcessingDataRecorded(decimal Value);

public record EventProcessingAnalytics(int EventCount, DateTimeOffset FirstEventTime, DateTimeOffset LastEventTime, decimal TotalValue);

public class EventProcessingAnalyticsReducer : IReducerFor<EventProcessingAnalytics>
{
    public EventProcessingAnalytics Recorded(EventProcessingDataRecorded @event, EventProcessingAnalytics? current, EventContext context)
    {
        if (current is null)
        {
            // First event - initialize state
            return new EventProcessingAnalytics(
                EventCount: 1,
                FirstEventTime: context.Occurred,
                LastEventTime: context.Occurred,
                TotalValue: @event.Value);
        }

        // Update existing state
        return current with
        {
            EventCount = current.EventCount + 1,
            LastEventTime = context.Occurred,
            TotalValue = current.TotalValue + @event.Value
        };
    }
}
```
