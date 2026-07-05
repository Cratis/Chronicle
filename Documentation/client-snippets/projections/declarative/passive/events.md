```csharp
using Cratis.Chronicle.Events;

[EventType]
public record DecPassiveUserCreated(string Name, string Email);

[EventType]
public record DecPassiveUserUpdated(string Name, string Email);

[EventType]
public record DecPassiveUserLoggedIn(DateTimeOffset LoginTime);
```
