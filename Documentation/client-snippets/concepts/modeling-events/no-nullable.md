```csharp
using Cratis.Chronicle.Events;

public record ModelingEventsOrderId(Guid Value);
public record ModelingEventsMoney(decimal Amount, string Currency);

// Nullable smell — "sometimes there's a discount, sometimes not"
[EventType]
public record ModelingEventsOrderPlacedWithNullableDiscount(
    ModelingEventsOrderId Id,
    ModelingEventsMoney Total,
    ModelingEventsMoney? Discount);

// Two facts
[EventType]
public record ModelingEventsOrderPlaced(ModelingEventsOrderId Id, ModelingEventsMoney Total);

[EventType]
public record ModelingEventsDiscountApplied(ModelingEventsOrderId Id, ModelingEventsMoney Amount);
```
