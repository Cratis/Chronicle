```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record NodRecursiveSliceCreated(string Name);

[EventType]
public record NodRecursiveCommandSet(string Name);

[EventType]
public record NodRecursiveCommandCleared;

[EventType]
public record NodRecursiveValidationConfigured(string Rules);

[EventType]
public record NodRecursiveValidationRemoved;

public record NodRecursiveSlice(
    string Name,
    NodRecursiveCommandItem? Command);

public record NodRecursiveCommandItem(
    string Name,
    NodRecursiveValidationItem? Validation);

public record NodRecursiveValidationItem(
    string Rules);

public class NodRecursiveSliceProjection : IProjectionFor<NodRecursiveSlice>
{
    public void Define(IProjectionBuilderFor<NodRecursiveSlice> builder) => builder
        .From<NodRecursiveSliceCreated>()
        .Nested(m => m.Command, nested => nested
            .From<NodRecursiveCommandSet>()
            .Nested(m => m.Validation, inner => inner
                .From<NodRecursiveValidationConfigured>()
                .ClearWith<NodRecursiveValidationRemoved>())
            .ClearWith<NodRecursiveCommandCleared>());
}
```
