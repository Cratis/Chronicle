```csharp
using Cratis.Chronicle.Events;

[EventType]
public record DecNotRewindableUserAction(
    string UserId,
    string ActionType,
    string Details);

[EventType]
public record DecNotRewindableSystemEvent(
    string ComponentName,
    string EventType,
    string Data);
```
