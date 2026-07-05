```csharp title="Events used by composite key projections"
using Cratis.Chronicle.Events;

[EventType]
public record CompositeOrderCreated(
    string CustomerId,
    string OrderNumber,
    string CustomerName,
    DateTimeOffset OrderDate);

[EventType]
public record CompositeOrderShipped(
    string CustomerId,
    string OrderNumber,
    DateTimeOffset ShippedDate);

[EventType]
public record CompositeUserAction(
    string UserId,
    string Action,
    string Details);
```
