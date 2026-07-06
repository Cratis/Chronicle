```csharp
using Cratis.Concepts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

public record ConstraintsModelBoundUniqueEventTypeUserId(Guid Value) : ConceptAs<Guid>(Value);

[EventType]
[RemoveConstraint("UniqueUser")]
public record ConstraintsModelBoundUniqueEventTypeUserRemoved(ConstraintsModelBoundUniqueEventTypeUserId UserId);
```
