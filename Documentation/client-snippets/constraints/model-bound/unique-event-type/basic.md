```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
[Unique]
public record ConstraintsModelBoundUniqueEventTypeUserRegistered(string Email, string DisplayName);
```
