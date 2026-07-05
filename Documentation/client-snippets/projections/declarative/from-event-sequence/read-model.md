```csharp
public record DecFromEventSequenceOrder(
    string OrderNumber,
    string CustomerId,
    decimal TotalAmount,
    DecFromEventSequenceOrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ShippedAt);

public enum DecFromEventSequenceOrderStatus
{
    Created,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
```
