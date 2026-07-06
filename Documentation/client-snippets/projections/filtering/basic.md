```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record FilteringOrderPlaced(string CustomerId, decimal TotalAmount);

[EventType]
public record FilteringOrderShipped(DateTimeOffset ShippedAt);

[FromEvent<FilteringOrderPlaced>]
[FromEvent<FilteringOrderShipped>]
public record FilteringOrderSummary(
    [Key] string CustomerId,
    decimal TotalAmount,
    DateTimeOffset? ShippedAt);
```
