```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecDictionarySeriesAssigned(string Slot, string SeriesId);

[EventType]
public record DecDictionarySlotCleared(string Slot);

public record DecDictionarySchedule(IDictionary<string, string> SeriesBySlot);

public class DecDictionaryScheduleProjection : IProjectionFor<DecDictionarySchedule>
{
    public void Define(IProjectionBuilderFor<DecDictionarySchedule> builder) => builder
        .From<DecDictionarySeriesAssigned>(_ => _
            .Set(m => m.SeriesBySlot, e => e.Slot).To(e => e.SeriesId))
        .From<DecDictionarySlotCleared>(_ => _
            .RemoveKey(m => m.SeriesBySlot, e => e.Slot));
}
```
