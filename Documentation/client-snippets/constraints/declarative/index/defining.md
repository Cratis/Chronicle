```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsDeclarativeIndexProjectCreated(string Name);

[EventType]
public record ConstraintsDeclarativeIndexProjectRemoved;

public class ConstraintsDeclarativeIndexUniqueProjectName : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<ConstraintsDeclarativeIndexProjectCreated>(e => e.Name)
                .RemovedWith<ConstraintsDeclarativeIndexProjectRemoved>());
}
```
