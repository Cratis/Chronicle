```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsModelBoundUniqueUserRegistered([property: Unique(name: "UniqueEmail")] string Email, string DisplayName);

[EventType]
public record ConstraintsModelBoundUniqueUserEmailChanged([property: Unique(name: "UniqueEmail")] string NewEmail);
```
