```csharp title="Clear a nested object from multiple events"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record CommandSetForNestedMultipleClear(string Name, string Schema);

[EventType]
public record CommandClearedForNestedMultipleClear;

[EventType]
public record SliceArchivedForNestedMultipleClear;

[FromEvent<CommandSetForNestedMultipleClear>]
[ClearWith<CommandClearedForNestedMultipleClear>]
[ClearWith<SliceArchivedForNestedMultipleClear>]
public record CommandItemNestedMultipleClear(
    string Name,
    string Schema);
```
