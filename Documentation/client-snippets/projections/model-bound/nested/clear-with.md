```csharp title="Clear a nested object"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record CommandSetForNestedClear(string Name, string Schema);

[EventType]
public record CommandClearedForNestedClear;

[FromEvent<CommandSetForNestedClear>]
[ClearWith<CommandClearedForNestedClear>]
public record CommandItemNestedClear(
    string Name,
    string Schema);
```
