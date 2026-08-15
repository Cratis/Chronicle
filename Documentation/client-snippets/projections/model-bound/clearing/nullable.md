```csharp title="A member has to be able to hold no value"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbClearingShiftPlanned(string Assignee, int Hours);

[EventType]
public record MbClearingShiftReleased;

[FromEvent<MbClearingShiftPlanned>]
public record MbClearingShift(
    [Key]
    Guid Id,

    // Nullable, so "nobody is assigned" is a state the member can actually hold.
    [ClearWith<MbClearingShiftReleased>]
    string? Assignee,

    // Nullable value type, for the same reason: 0 hours is a number of hours, not the absence of one.
    [ClearWith<MbClearingShiftReleased>]
    int? Hours);
```
