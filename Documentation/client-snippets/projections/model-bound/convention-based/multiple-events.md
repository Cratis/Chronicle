```csharp title="Multiple convention events"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record ConventionUserProfileCreated(string Name, string Email);

[EventType]
public record ConventionUserProfileUpdated(string Name, string Email, string Phone);

[FromEvent<ConventionUserProfileCreated>]
[FromEvent<ConventionUserProfileUpdated>]
public record ConventionUserProfile(
    [Key] Guid Id,
    string Name,
    string Email,
    string Phone);
```
