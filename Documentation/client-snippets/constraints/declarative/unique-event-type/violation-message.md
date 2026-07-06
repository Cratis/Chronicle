```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueEventTypeMessageProjectInitialized;

public class ConstraintsUniqueEventTypeMessageProjectInitialization : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<ConstraintsUniqueEventTypeMessageProjectInitialized>(
            message: "A project can only be initialized once.");
}
```
