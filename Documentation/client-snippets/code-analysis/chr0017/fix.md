```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record Chr0017UserRegisteredFixed;

public class Chr0017UniqueEmailConstraintFixed : IConstraint
{
    // Now valid - no constructor dependencies
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<Chr0017UserRegisteredFixed>();
}
```
