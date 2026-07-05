```csharp title="Combine specific mappings with every-event metadata"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record UserRegisteredForEvery(string Name, string Email);

[EventType]
public record UserNameChangedForEvery(string NewName);

[EventType]
public record UserEmailChangedForEvery(string NewEmail);

[FromEvent<UserRegisteredForEvery>]
[FromEvent<UserNameChangedForEvery>]
[FromEvent<UserEmailChangedForEvery>]
public record UserProfileFromEvery(
    [Key] Guid Id,
    [SetFrom<UserRegisteredForEvery>(nameof(UserRegisteredForEvery.Name))]
    [SetFrom<UserNameChangedForEvery>(nameof(UserNameChangedForEvery.NewName))]
    string Name,
    [SetFrom<UserRegisteredForEvery>(nameof(UserRegisteredForEvery.Email))]
    [SetFrom<UserEmailChangedForEvery>(nameof(UserEmailChangedForEvery.NewEmail))]
    string Email,
    [FromEvery(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset LastUpdated);
```
