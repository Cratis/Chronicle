```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueSeveralInvitationSent(string EmailAddress);

[EventType]
public record ConstraintsUniqueSeveralInvitationAccepted;

[EventType]
public record ConstraintsUniqueSeveralInvitationRevoked;

[EventType]
public record ConstraintsUniqueSeveralInvitationExpired;

public class ConstraintsUniqueSeveralInvitedAddress : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<ConstraintsUniqueSeveralInvitationSent>(e => e.EmailAddress)
                .RemovedWith<ConstraintsUniqueSeveralInvitationAccepted>()
                .RemovedWith<ConstraintsUniqueSeveralInvitationRevoked>()
                .RemovedWith<ConstraintsUniqueSeveralInvitationExpired>());
}
```
