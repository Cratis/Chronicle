```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueNamedUserRegistered(string Email);

public class ConstraintsUniqueNamedEmail : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .WithName("UniqueEmail")
                .On<ConstraintsUniqueNamedUserRegistered>(e => e.Email));
}
```
