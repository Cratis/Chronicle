```csharp title="Multiple nested events"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record SliceCreatedForNestedUpdates(string Name);

[EventType]
public record CommandSetForNestedUpdates(string Name, string Schema);

[EventType]
public record CommandRenamedForNestedUpdates(string NewName);

[EventType]
public record CommandSchemaUpdatedForNestedUpdates(string UpdatedSchema);

[EventType]
public record CommandClearedForNestedUpdates;

public record SliceForNestedUpdates(
    string Name,
    CommandItemForNestedUpdates? Command);

public record CommandItemForNestedUpdates(
    string Name,
    string Schema);

public class SliceProjectionForNestedUpdates : IProjectionFor<SliceForNestedUpdates>
{
    public void Define(IProjectionBuilderFor<SliceForNestedUpdates> builder) => builder
        .From<SliceCreatedForNestedUpdates>()
        .Nested(m => m.Command, nested => nested
            .From<CommandSetForNestedUpdates>()
            .From<CommandRenamedForNestedUpdates>(b => b
                .Set(m => m.Name).To(e => e.NewName))
            .From<CommandSchemaUpdatedForNestedUpdates>(b => b
                .Set(m => m.Schema).To(e => e.UpdatedSchema))
            .ClearWith<CommandClearedForNestedUpdates>());
}
```
