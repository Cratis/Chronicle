```csharp
using Cratis.Concepts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

public record ConstraintsModelBoundUniqueUserId(Guid Value) : ConceptAs<Guid>(Value);

[EventType]
[RemoveConstraint("UniqueEmail")]
public record ConstraintsModelBoundUniqueUserRemoved(ConstraintsModelBoundUniqueUserId UserId);
```
