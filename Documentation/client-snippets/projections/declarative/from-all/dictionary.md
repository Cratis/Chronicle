```csharp title="Declarative FromAll with a dynamic dictionary key"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record UserRegisteredForEventCounts(string Name);

[EventType]
public record OrderPlacedForEventCounts(string OrderId);

public record EventTypeCountsReadModel(
    Guid Id,
    Dictionary<string, int> EventCountByType,
    DateTimeOffset LastEventOccurred);

public class EventTypeCountsProjection : IProjectionFor<EventTypeCountsReadModel>
{
    public void Define(IProjectionBuilderFor<EventTypeCountsReadModel> builder) => builder
        .FromAll(_ => _
            .Count(m => m.EventCountByType, c => c.EventType.Id)
            .Set(m => m.LastEventOccurred).ToEventContextProperty(c => c.Occurred));
}
```
