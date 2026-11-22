```csharp title="Map the event source identifier"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record OrderPlacedForAudit(string CustomerName);

public record OrderSource(
    [Key] Guid Id,
    [SetFromEventSourceId<OrderPlacedForAudit>] EventSourceId Source);
```
