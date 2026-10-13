```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
[Unique]
public record AccountOpened(string Owner);
```
