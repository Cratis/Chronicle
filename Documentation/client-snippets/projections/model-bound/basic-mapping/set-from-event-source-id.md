```csharp title="Map the event source identifier"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record OrderPlacedWithSourceId(string CustomerName);

public record OrderEventSourceReference(
    [Key] Guid Id,
    [SetFromEventSourceId<OrderPlacedWithSourceId>] EventSourceId Source);
```
