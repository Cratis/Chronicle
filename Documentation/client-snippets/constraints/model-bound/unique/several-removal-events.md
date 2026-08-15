```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
[RemoveConstraint("UniqueInvitedEmail")]
public record ConstraintsModelBoundUniqueSeveralInvitationAccepted;

[EventType]
[RemoveConstraint("UniqueInvitedEmail")]
public record ConstraintsModelBoundUniqueSeveralInvitationRevoked;

[EventType]
[RemoveConstraint("UniqueInvitedEmail")]
public record ConstraintsModelBoundUniqueSeveralInvitationExpired;
```
