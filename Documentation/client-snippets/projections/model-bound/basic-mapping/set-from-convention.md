```csharp title="Convention-based set mapping"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record UserRegisteredForProfile(string Name, string Email);

[FromEvent<UserRegisteredForProfile>]
public record UserProfile(
    [Key] Guid Id,
    string Name,
    string Email);
```
