```csharp
using Cratis.Chronicle.Events;

[EventType]
public record DecFromEventSequenceOrderCreated(
    string OrderNumber,
    string CustomerId,
    decimal TotalAmount);

[EventType]
public record DecFromEventSequenceOrderUpdated(
    string OrderNumber,
    decimal NewTotalAmount);

[EventType]
public record DecFromEventSequenceOrderShipped(
    string OrderNumber,
    DateTimeOffset ShippedAt);
```
