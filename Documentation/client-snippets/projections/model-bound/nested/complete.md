```csharp title="Complete nested object projection"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record SliceCreatedForNestedComplete(string Name);

[EventType]
public record CommandSetForNestedComplete(
    Guid CommandId,
    string Name,
    string Schema,
    string Rules,
    string StateSchema);

[EventType]
public record CommandRenamedForNestedComplete(Guid CommandId, string Name);

[EventType]
public record CommandDefinitionUpdatedForNestedComplete(
    Guid CommandId,
    string Schema,
    string Rules,
    string StateSchema);

[EventType]
public record CommandClearedForNestedComplete;

[FromEvent<SliceCreatedForNestedComplete>]
public record SliceNestedComplete(
    [Key] Guid Id,
    string Name,
    [Nested] CommandItemNestedComplete? Command);

[FromEvent<CommandSetForNestedComplete>]
[FromEvent<CommandRenamedForNestedComplete>]
[FromEvent<CommandDefinitionUpdatedForNestedComplete>]
[ClearWith<CommandClearedForNestedComplete>]
public record CommandItemNestedComplete(
    [SetFrom<CommandSetForNestedComplete>(nameof(CommandSetForNestedComplete.CommandId))]
    Guid Id,
    [SetFrom<CommandSetForNestedComplete>(nameof(CommandSetForNestedComplete.Name))]
    [SetFrom<CommandRenamedForNestedComplete>(nameof(CommandRenamedForNestedComplete.Name))]
    string Name,
    [SetFrom<CommandSetForNestedComplete>(nameof(CommandSetForNestedComplete.Schema))]
    [SetFrom<CommandDefinitionUpdatedForNestedComplete>(nameof(CommandDefinitionUpdatedForNestedComplete.Schema))]
    string Schema,
    [SetFrom<CommandSetForNestedComplete>(nameof(CommandSetForNestedComplete.Rules))]
    [SetFrom<CommandDefinitionUpdatedForNestedComplete>(nameof(CommandDefinitionUpdatedForNestedComplete.Rules))]
    string Rules,
    [SetFrom<CommandSetForNestedComplete>(nameof(CommandSetForNestedComplete.StateSchema))]
    [SetFrom<CommandDefinitionUpdatedForNestedComplete>(nameof(CommandDefinitionUpdatedForNestedComplete.StateSchema))]
    string StateSchema);
```
