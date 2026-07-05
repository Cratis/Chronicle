```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record EventProcessingMinimalMetricRecorded(decimal Value);

public record EventProcessingMinimalStats(int Count, decimal Sum);

public class EventProcessingMinimalStatsReducer : IReducerFor<EventProcessingMinimalStats>
{
    // Efficient - only creates a new object when needed
    public EventProcessingMinimalStats Recorded(EventProcessingMinimalMetricRecorded @event, EventProcessingMinimalStats? current)
    {
        if (current is null)
            return new EventProcessingMinimalStats(Count: 1, Sum: @event.Value);

        return current with
        {
            Count = current.Count + 1,
            Sum = current.Sum + @event.Value
        };
    }
}
```
