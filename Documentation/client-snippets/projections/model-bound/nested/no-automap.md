```csharp title="Disable AutoMap on a nested type"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record CommandSetForNestedNoAutoMap(string CommandName, string Schema);

[EventType]
public record CommandClearedForNestedNoAutoMap;

[FromEvent<CommandSetForNestedNoAutoMap>]
[ClearWith<CommandClearedForNestedNoAutoMap>]
[NoAutoMap]
public record CommandItemNestedNoAutoMap(
    [SetFrom<CommandSetForNestedNoAutoMap>(nameof(CommandSetForNestedNoAutoMap.CommandName))]
    string Name,
    [SetFrom<CommandSetForNestedNoAutoMap>(nameof(CommandSetForNestedNoAutoMap.Schema))]
    string Schema);
```
