```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecEventContextUserActionForKey(string UserId, string ActionType);

public record DecEventContextEventLogKey(DateTimeOffset Date, ulong SequenceNumber);

public record DecEventContextEventLogEntry(
    DecEventContextEventLogKey Id,
    string ActionType,
    string UserId);

public class DecEventContextEventLogProjection : IProjectionFor<DecEventContextEventLogEntry>
{
    public void Define(IProjectionBuilderFor<DecEventContextEventLogEntry> builder) => builder
        .From<DecEventContextUserActionForKey>(_ => _
            .UsingCompositeKey<DecEventContextEventLogKey>(_ => _
                .Set(k => k.Date).ToEventContextProperty(c => c.Occurred)
                .Set(k => k.SequenceNumber).ToEventContextProperty(c => c.SequenceNumber))
            .Set(m => m.ActionType).To(e => e.ActionType)
            .Set(m => m.UserId).To(e => e.UserId));
}
```
