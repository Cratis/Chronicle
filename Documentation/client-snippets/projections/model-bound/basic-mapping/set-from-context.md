```csharp title="Map event context"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record OrderPlacedForAudit(string CustomerName);

public record AuditedOrder(
    [Key] Guid Id,

    [SetFrom<OrderPlacedForAudit>(nameof(OrderPlacedForAudit.CustomerName))]
    string CustomerName,

    [SetFromContext<OrderPlacedForAudit>(nameof(EventContext.Occurred))]
    DateTimeOffset OrderedAt);
```
