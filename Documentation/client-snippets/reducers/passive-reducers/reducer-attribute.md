```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record PassiveReducersDataRecorded(decimal Value);

public record PassiveReducersAnalytics(int RecordCount, decimal TotalValue, DateTimeOffset LastUpdated);

[Reducer(isActive: false)]
public class PassiveReducersTemporaryAnalyticsReducer : IReducerFor<PassiveReducersAnalytics>
{
    public PassiveReducersAnalytics Recorded(PassiveReducersDataRecorded @event, PassiveReducersAnalytics? current, EventContext context)
    {
        var count = current?.RecordCount ?? 0;
        var sum = current?.TotalValue ?? 0m;

        return new PassiveReducersAnalytics(count + 1, sum + @event.Value, context.Occurred);
    }
}
```
