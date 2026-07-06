```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record EventProcessingHourlyMetricRecorded(decimal Value);

public record EventProcessingHourlyMetrics(Dictionary<int, decimal> MetricsByHour);

public class EventProcessingHourlyMetricsReducer : IReducerFor<EventProcessingHourlyMetrics>
{
    public EventProcessingHourlyMetrics Recorded(EventProcessingHourlyMetricRecorded @event, EventProcessingHourlyMetrics? current, EventContext context)
    {
        var metricsByHour = new Dictionary<int, decimal>(current?.MetricsByHour ?? []);
        var hour = context.Occurred.Hour;

        if (!metricsByHour.ContainsKey(hour))
            metricsByHour[hour] = 0;

        metricsByHour[hour] += @event.Value;

        return new EventProcessingHourlyMetrics(metricsByHour);
    }
}
```
