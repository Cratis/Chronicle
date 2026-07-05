```csharp title="Equivalent explicit mappings"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record ExplicitConventionUserRegistered(
    string Name,
    string Email,
    DateTimeOffset RegisteredAt);

public record ExplicitConventionUser(
    [Key] Guid Id,

    [SetFrom<ExplicitConventionUserRegistered>(nameof(ExplicitConventionUserRegistered.Name))]
    string Name,

    [SetFrom<ExplicitConventionUserRegistered>(nameof(ExplicitConventionUserRegistered.Email))]
    string Email,

    [SetFrom<ExplicitConventionUserRegistered>(nameof(ExplicitConventionUserRegistered.RegisteredAt))]
    DateTimeOffset RegisteredAt);
```
