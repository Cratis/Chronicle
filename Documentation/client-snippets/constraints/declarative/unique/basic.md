```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueProjectCreated(string Name);

[EventType]
public record ConstraintsUniqueProjectRemoved;

public class ConstraintsUniqueProjectName : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<ConstraintsUniqueProjectCreated>(e => e.Name)
                .RemovedWith<ConstraintsUniqueProjectRemoved>());
}
```
