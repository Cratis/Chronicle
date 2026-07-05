```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record EventProcessingMetricRecorded(decimal Value);

public record EventProcessingStatistics(decimal Sum, int Count, decimal Average);

public class EventProcessingStatisticsReducer : IReducerFor<EventProcessingStatistics>
{
    public EventProcessingStatistics Recorded(EventProcessingMetricRecorded @event, EventProcessingStatistics? current)
    {
        var sum = (current?.Sum ?? 0) + @event.Value;
        var count = (current?.Count ?? 0) + 1;

        return new EventProcessingStatistics(sum, count, sum / count);
    }
}
```
