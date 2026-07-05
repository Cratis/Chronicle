```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbNotRewindableAuditEvent(string Message, DateTimeOffset OccurredAt);

[NotRewindable]
public record MbNotRewindableAuditLog(
    [Key]
    Guid Id,

    [SetFrom<MbNotRewindableAuditEvent>(nameof(MbNotRewindableAuditEvent.Message))]
    string Message,

    [SetFrom<MbNotRewindableAuditEvent>(nameof(MbNotRewindableAuditEvent.OccurredAt))]
    DateTimeOffset Timestamp);
```
