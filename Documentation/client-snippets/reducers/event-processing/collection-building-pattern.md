```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record EventProcessingCustomerAction(string Type, string Description);

public record EventProcessingActivity(string Type, DateTimeOffset Timestamp, string Description);
public record EventProcessingCustomerActivityLog(List<EventProcessingActivity> Activities);

public class EventProcessingCustomerActivityLogReducer : IReducerFor<EventProcessingCustomerActivityLog>
{
    public EventProcessingCustomerActivityLog Recorded(EventProcessingCustomerAction @event, EventProcessingCustomerActivityLog? current, EventContext context)
    {
        // Copy rather than mutate — current.Activities may still be referenced by a held snapshot
        var activities = new List<EventProcessingActivity>(current?.Activities ?? []);

        activities.Add(new EventProcessingActivity(
            @event.Type,
            context.Occurred,
            @event.Description));

        return new EventProcessingCustomerActivityLog(activities);
    }
}
```
