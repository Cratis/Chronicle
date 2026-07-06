```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
[Unique(name: "UniqueUser", message: "A user with this identity has already been registered.")]
public record ConstraintsModelBoundUniqueEventTypeNamedUserRegistered(string Email, string DisplayName);
```
