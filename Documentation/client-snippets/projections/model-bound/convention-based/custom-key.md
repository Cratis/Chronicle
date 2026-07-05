```csharp title="Custom key"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record ConventionUserRegisteredWithKey(
    Guid UserId,
    string Name,
    string Email);

[FromEvent<ConventionUserRegisteredWithKey>(key: nameof(ConventionUserRegisteredWithKey.UserId))]
public record ConventionUserById(
    [Key] Guid Id,
    string Name,
    string Email);
```
