```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueCasingUserRegistered(string Email);

public class ConstraintsUniqueCasingEmail : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<ConstraintsUniqueCasingUserRegistered>(e => e.Email)
                .IgnoreCasing());
}
```
