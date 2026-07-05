```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record NodSliceCreated(string Name);

[EventType]
public record NodCommandSetForSlice(string Name, string Schema);

[EventType]
public record NodCommandClearedForSlice;

[FromEvent<NodSliceCreated>]
public record NodSlice(
    [Key] Guid Id,
    string Name,
    [Nested] NodCommandItem? Command);

[FromEvent<NodCommandSetForSlice>]
[ClearWith<NodCommandClearedForSlice>]
public record NodCommandItem(
    string Name,
    string Schema);
```
