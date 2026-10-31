```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record MappingVersionRegistered(string VersionId);

[EventType]
public record MappingVersionRemoved(string VersionId);

[EventType]
public record MappingVersionsRemoved;

public class UniqueMappingVersionId : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique => unique
            .WithMode(UniqueConstraintMode.PerValue)
            .On<MappingVersionRegistered>(e => e.VersionId)
            .RemovedWith<MappingVersionRemoved>(e => e.VersionId)
            .RemovedWith<MappingVersionsRemoved>());
}
```
