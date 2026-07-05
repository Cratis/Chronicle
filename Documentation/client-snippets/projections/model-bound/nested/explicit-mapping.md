```csharp title="Explicit mappings on a nested type"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record CommandSetForNestedExplicit(string CommandName, string JsonSchema);

[EventType]
public record CommandSchemaUpdatedForNestedExplicit(string UpdatedSchema);

[EventType]
public record CommandClearedForNestedExplicit;

[FromEvent<CommandSetForNestedExplicit>]
[FromEvent<CommandSchemaUpdatedForNestedExplicit>]
[ClearWith<CommandClearedForNestedExplicit>]
public record CommandItemNestedExplicit(
    [SetFrom<CommandSetForNestedExplicit>(nameof(CommandSetForNestedExplicit.CommandName))]
    string Name,
    [SetFrom<CommandSetForNestedExplicit>(nameof(CommandSetForNestedExplicit.JsonSchema))]
    [SetFrom<CommandSchemaUpdatedForNestedExplicit>(nameof(CommandSchemaUpdatedForNestedExplicit.UpdatedSchema))]
    string Schema);
```
