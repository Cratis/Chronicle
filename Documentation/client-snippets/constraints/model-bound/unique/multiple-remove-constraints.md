```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
[RemoveConstraint("UniqueEmail")]
[RemoveConstraint("UniqueUsername")]
public record ConstraintsModelBoundUniqueMultiRemoveUserRemoved(ConstraintsModelBoundUniqueUserId UserId);
```
