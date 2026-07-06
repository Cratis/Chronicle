```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueMessageProjectCreated(string Name);

public class ConstraintsUniqueMessageProjectName : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<ConstraintsUniqueMessageProjectCreated>(e => e.Name)
                .WithMessage("A project with this name already exists."));
}
```
