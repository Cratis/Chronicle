```csharp
[ReadModel]
public record IndexingOrder(
    Guid Id,
    [Index] Guid CustomerId,
    [Index] string OrderNumber,
    decimal Total);
```
