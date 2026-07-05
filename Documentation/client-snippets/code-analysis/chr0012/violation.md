```csharp
using Cratis.Chronicle.Events;

[EventType]
public record Chr0012MissionAccepted(
    string MissionId,
    DateOnly? StartDate);
```
