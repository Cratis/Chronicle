```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueEventTypeProjectInitialized;

public class ConstraintsUniqueEventTypeProjectInitialization : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<ConstraintsUniqueEventTypeProjectInitialized>();
}
```
