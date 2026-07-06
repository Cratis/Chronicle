```csharp title="Update a nested object from multiple events"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record CommandSetForNestedMultipleFrom(string Name, string Schema);

[EventType]
public record CommandRenamedForNestedMultipleFrom(string Name);

[EventType]
public record CommandSchemaUpdatedForNestedMultipleFrom(string Schema);

[EventType]
public record CommandClearedForNestedMultipleFrom;

[FromEvent<CommandSetForNestedMultipleFrom>]
[FromEvent<CommandRenamedForNestedMultipleFrom>]
[FromEvent<CommandSchemaUpdatedForNestedMultipleFrom>]
[ClearWith<CommandClearedForNestedMultipleFrom>]
public record CommandItemNestedMultipleFrom(
    string Name,
    string Schema);
```
