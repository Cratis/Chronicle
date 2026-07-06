```csharp title="Nested object lifecycle"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record CommandSetForNestedBasic(string Name, string Schema);

[EventType]
public record CommandClearedForNestedBasic;

[FromEvent<CommandSetForNestedBasic>]
public record SliceWithNestedCommandBasic(
    [Key] Guid Id,
    string Name,
    [Nested] CommandItemNestedBasic? Command);

[FromEvent<CommandSetForNestedBasic>]
[ClearWith<CommandClearedForNestedBasic>]
public record CommandItemNestedBasic(
    string Name,
    string Schema);
```
