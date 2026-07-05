```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record UcDedicatedUserRegistered(string Email, string DisplayName);

[EventType]
public record UcDedicatedUserEmailChanged(string NewEmail);

[EventType]
public record UcDedicatedUserRemoved;

public class UcDedicatedUniqueEmail : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .WithName("UniqueEmail")
                .On<UcDedicatedUserRegistered>(e => e.Email)
                .On<UcDedicatedUserEmailChanged>(e => e.NewEmail)
                .IgnoreCasing()
                .RemovedWith<UcDedicatedUserRemoved>()
                .WithMessage("That email address is already in use."));
}
```
