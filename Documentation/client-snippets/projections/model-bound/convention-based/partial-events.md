```csharp title="Partial event shapes"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record ConventionPartialUserRegistered(string Email);

[EventType]
public record ConventionPartialUserCompleted(
    string FirstName,
    string LastName,
    string Phone);

[FromEvent<ConventionPartialUserRegistered>]
[FromEvent<ConventionPartialUserCompleted>]
public record ConventionPartialUser(
    [Key] Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Phone);
```
