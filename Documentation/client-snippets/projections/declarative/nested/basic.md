```csharp title="Nested object projection"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record SliceCreatedForNestedBasic(string Name);

[EventType]
public record CommandSetForDeclarativeNestedBasic(string Name, string Schema);

[EventType]
public record CommandClearedForDeclarativeNestedBasic;

public record SliceForNestedBasic(
    string Name,
    CommandItemForNestedBasic? Command);

public record CommandItemForNestedBasic(
    string Name,
    string Schema);

public class SliceProjectionForNestedBasic : IProjectionFor<SliceForNestedBasic>
{
    public void Define(IProjectionBuilderFor<SliceForNestedBasic> builder) => builder
        .From<SliceCreatedForNestedBasic>()
        .Nested(m => m.Command, nested => nested
            .From<CommandSetForDeclarativeNestedBasic>()
            .ClearWith<CommandClearedForDeclarativeNestedBasic>());
}
```
