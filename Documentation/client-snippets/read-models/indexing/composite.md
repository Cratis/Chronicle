```csharp
using Cratis.Chronicle.ReadModels;

[ReadModel]
[CompositeIndex(nameof(CustomerId), nameof(CreatedAt), IsDescending = new[] { false, true })]
public record ReadModelsIndexingRecentOrders(
    Guid Id,
    Guid CustomerId,
    DateTimeOffset CreatedAt);
```
