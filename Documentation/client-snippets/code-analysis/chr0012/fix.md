```csharp
using Cratis.Chronicle.Events;

[EventType]
public record Chr0012MissionAcceptedFixed(
    string MissionId,
    DateOnly StartDate);

[EventType]
public record Chr0012MissionAcceptedWithoutStartDate(
    string MissionId);
```
