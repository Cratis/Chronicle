```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueMessageCallbackProjectCreated(string Name);

public class ConstraintsUniqueMessageCallbackProjectName : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<ConstraintsUniqueMessageCallbackProjectCreated>(e => e.Name)
                .WithMessage(violation => $"A project named '{violation.Details[WellKnownConstraintDetailKeys.PropertyValue]}' already exists."));
}
```
