```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[EventType]
[Tag("analytics", "user-action")]
public record TaggingUserLoggedIn(string UserId, DateTimeOffset LoggedInAt);

// [Tags] (plural) is equivalent to [Tag] — use whichever reads more naturally
[EventType]
[Tags("analytics", "user-action")]
public record TaggingUserLoggedInAlternate(string UserId, DateTimeOffset LoggedInAt);

// Mixing [Tag] and [Tags] on the same type merges all the tags
[EventType]
[Tag("security")]
[Tags("audit")]
public record TaggingUserPasswordChanged(string UserId, DateTimeOffset ChangedAt);
```
