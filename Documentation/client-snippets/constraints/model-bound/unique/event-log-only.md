```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
[RemoveConstraint("ConstraintsModelBoundUniqueEventLogInvitationEmail")]
public record ConstraintsModelBoundUniqueEventLogInvitationRevoked;

[EventType]
public record ConstraintsModelBoundUniqueEventLogInvitationSent(
    [property: Unique("ConstraintsModelBoundUniqueEventLogInvitationEmail", EventSequences = [EventSequenceId.LogId])] string Email);
```
