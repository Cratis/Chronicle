```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecEventContextActivityPerformed(string ActivityId, string ActivityType);

public record DecEventContextActivityLogEntry(
    string ActivityId,
    DateTimeOffset Timestamp,
    ulong SequenceNumber);

public record DecEventContextUserWithActivityLog(
    IEnumerable<DecEventContextActivityLogEntry> ActivityLog);

public class DecEventContextUserActivityLogProjection : IProjectionFor<DecEventContextUserWithActivityLog>
{
    public void Define(IProjectionBuilderFor<DecEventContextUserWithActivityLog> builder) => builder
        .Children(m => m.ActivityLog, children => children
            .IdentifiedBy(e => e.ActivityId)
            .AutoMap()
            .From<DecEventContextActivityPerformed>(_ => _
                .UsingKey(e => e.ActivityId)
                .Set(m => m.Timestamp).ToEventContextProperty(c => c.Occurred)
                .Set(m => m.SequenceNumber).ToEventContextProperty(c => c.SequenceNumber)));
}
```
