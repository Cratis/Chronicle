```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record Chr0018UserRegisteredFixed;

public class Chr0018UniqueEmailConstraintFixed : IConstraint
{
    // Now valid - unconditional
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<Chr0018UserRegisteredFixed>();
}
```
