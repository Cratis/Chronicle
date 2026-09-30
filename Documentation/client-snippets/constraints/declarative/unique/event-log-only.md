```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueEventLogInvitationSent(string Email);

[EventType]
public record ConstraintsUniqueEventLogInvitationRevoked;

public class ConstraintsUniqueEventLogInvitationEmail : IConstraint
{
    // InvitationSent is also forwarded to the outbox. Scope the constraint to the event log
    // so the forwarded copy does not claim the email in the outbox's own index.
    public void Define(IConstraintBuilder builder) =>
        builder
            .ForEventLog()
            .Unique(unique =>
                unique
                    .On<ConstraintsUniqueEventLogInvitationSent>(e => e.Email)
                    .RemovedWith<ConstraintsUniqueEventLogInvitationRevoked>());
}
```
