```csharp title="Model-bound set mapping"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record UserRegisteredForContact(string Name, string Email);

public record UserContact(
    [Key] Guid Id,

    [SetFrom<UserRegisteredForContact>(nameof(UserRegisteredForContact.Email))]
    string Email,

    [SetFrom<UserRegisteredForContact>(nameof(UserRegisteredForContact.Name))]
    string Name);
```
