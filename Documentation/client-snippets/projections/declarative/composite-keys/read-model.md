```csharp title="Read model with composite key"
public record CompositeOrder(
    CompositeOrderKey Id,
    string CustomerName,
    DateTimeOffset OrderDate,
    DateTimeOffset? ShippedDate);
```
