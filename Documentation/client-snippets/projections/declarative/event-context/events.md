```csharp
using Cratis.Chronicle.Events;

[EventType]
public record DecEventContextUserLoggedIn(string Username);

[EventType]
public record DecEventContextUserPerformedAction(string UserId, string ActionType);
```
