```csharp
public record ReducersGettingStartedOrderSummary(
    Guid OrderId,
    decimal TotalAmount,
    int ItemCount,
    DateTimeOffset LastUpdated);
```
