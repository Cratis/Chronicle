```csharp title="Specific context vs every event"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record OrderPlacedForLifecycle(string CustomerName);

[EventType]
public record OrderShippedForLifecycle(string TrackingNumber);

public record OrderLifecycle(
    [Key] Guid Id,

    [SetFromContext<OrderPlacedForLifecycle>(nameof(EventContext.Occurred))]
    DateTimeOffset PlacedAt,

    [SetFromContext<OrderShippedForLifecycle>(nameof(EventContext.Occurred))]
    DateTimeOffset? ShippedAt,

    [FromEvery(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset LastModified);
```
