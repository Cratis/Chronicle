```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueUserRegistered(string Email);

[EventType]
public record ConstraintsUniqueUserEmailChanged(string NewEmail);

[EventType]
public record ConstraintsUniqueUserRemoved;

public class ConstraintsUniqueEmailAcrossEvents : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .WithName("UniqueEmail")
                .On<ConstraintsUniqueUserRegistered>(e => e.Email)
                .On<ConstraintsUniqueUserEmailChanged>(e => e.NewEmail)
                .RemovedWith<ConstraintsUniqueUserRemoved>());
}
```
