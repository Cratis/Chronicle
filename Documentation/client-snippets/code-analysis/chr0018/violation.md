```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record Chr0018UserRegistered;

public class Chr0018UniqueEmailConstraint : IConstraint
{
    public bool SomeCondition { get; init; }

    // CHR0018: Define must not contain conditional logic
    public void Define(IConstraintBuilder builder)
    {
        if (SomeCondition)
        {
            builder.Unique<Chr0018UserRegistered>();
        }
    }
}
```
