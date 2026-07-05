```csharp title="AutoMap in a nested scope"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record SliceCreatedForNestedAutoMap(string Name);

[EventType]
public record CommandSetForNestedAutoMap(string Name, string Schema);

[EventType]
public record CommandUpdatedForNestedAutoMap(string Schema);

[EventType]
public record CommandClearedForNestedAutoMap;

public record SliceForNestedAutoMap(
    string Name,
    CommandItemForNestedAutoMap? Command);

public record CommandItemForNestedAutoMap(
    string Name,
    string Schema);

public class SliceProjectionForNestedAutoMap : IProjectionFor<SliceForNestedAutoMap>
{
    public void Define(IProjectionBuilderFor<SliceForNestedAutoMap> builder) => builder
        .From<SliceCreatedForNestedAutoMap>()
        .Nested(m => m.Command, nested => nested
            .From<CommandSetForNestedAutoMap>()
            .From<CommandUpdatedForNestedAutoMap>()
            .ClearWith<CommandClearedForNestedAutoMap>());
}
```
