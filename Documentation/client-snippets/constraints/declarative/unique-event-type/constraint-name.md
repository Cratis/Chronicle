```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueEventTypeNamedProjectInitialized;

public class ConstraintsUniqueEventTypeNamedProjectInitialization : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<ConstraintsUniqueEventTypeNamedProjectInitialized>(
            name: "UniqueProjectInitialization",
            message: "A project can only be initialized once.");
}
```
