```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueEventTypeCallbackProjectInitialized;

public class ConstraintsUniqueEventTypeCallbackProjectInitialization : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<ConstraintsUniqueEventTypeCallbackProjectInitialized>(
            messageCallback: violation => $"Constraint '{violation.ConstraintName}' was violated - the project has already been initialized.");
}
```
