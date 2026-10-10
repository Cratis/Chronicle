```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ScheduleVersionRegistered(
    [property: Unique("ScheduleVersionId", Mode = UniqueConstraintMode.PerValue)] string VersionId);

[EventType]
[RemoveConstraint("ScheduleVersionId", Properties = [nameof(VersionId)])]
public record ScheduleVersionRemoved(string VersionId);

[EventType]
[RemoveConstraint("ScheduleVersionId")]
public record ScheduleVersionsRemoved;
```
