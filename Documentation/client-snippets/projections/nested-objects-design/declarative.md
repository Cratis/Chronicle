```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record NodDeclarativeSliceCreated(string Name);

[EventType]
public record NodDeclarativeCommandSet(string Name, string Schema);

[EventType]
public record NodDeclarativeCommandCleared;

public record NodDeclarativeSlice(
    string Name,
    NodDeclarativeCommandItem? Command);

public record NodDeclarativeCommandItem(
    string Name,
    string Schema);

public class NodDeclarativeSliceProjection : IProjectionFor<NodDeclarativeSlice>
{
    public void Define(IProjectionBuilderFor<NodDeclarativeSlice> builder) => builder
        .From<NodDeclarativeSliceCreated>()
        .Nested(m => m.Command, nested => nested
            .From<NodDeclarativeCommandSet>()
            .ClearWith<NodDeclarativeCommandCleared>());
}
```
