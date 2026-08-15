```csharp title="Clear a scalar member"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbClearingProjectNoted(string Note);

[EventType]
public record MbClearingProjectNoteCleared;

[FromEvent<MbClearingProjectNoted>]
public record MbClearingProjectNotes(
    [Key]
    Guid Id,

    [ClearWith<MbClearingProjectNoteCleared>]
    string? Note);
```
