```csharp
using Cratis.Concepts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

public record UcUserId(Guid Value) : ConceptAs<Guid>(Value);

[EventType]
[RemoveConstraint("UniqueEmail")]
public record UcUserRemoved(UcUserId UserId);
```
