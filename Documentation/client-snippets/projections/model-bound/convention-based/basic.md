```csharp title="Convention-based mapping"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record ConventionUserRegistered(
    string Name,
    string Email,
    DateTimeOffset RegisteredAt);

[FromEvent<ConventionUserRegistered>]
public record ConventionUser(
    [Key] Guid Id,
    string Name,
    string Email,
    DateTimeOffset RegisteredAt);
```
