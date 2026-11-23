```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record Chr0040OrderPlaced(decimal Amount);

// Warning CHR0040: 'PlacedAt' carries more than one context-sourced mapping
// ([SetFromContext<Chr0040OrderPlaced>] or [SetFromEventSourceId<Chr0040OrderPlaced>]),
// and they map to the same property - only the last declared is kept. The CorrelationId
// capture is silently dropped: a single member cannot hold both context values. Move it
// onto its own property instead. Duplicate [SetFromEventSourceId] attributes or a mix
// of both attributes for the same event type also trigger this warning.
[FromEvent<Chr0040OrderPlaced>]
public record Chr0040Order(
    [Key] Guid Id,
    decimal Amount,
    [SetFromContext<Chr0040OrderPlaced>(nameof(EventContext.CorrelationId))]
    [SetFromContext<Chr0040OrderPlaced>(nameof(EventContext.Occurred))]
    DateTimeOffset PlacedAt);
```
