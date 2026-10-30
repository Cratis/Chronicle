```csharp
using Cratis.Chronicle.ReadModels;

[ReadModel]
public record ReadModelsIndexingLatestOrders(
    Guid Id,
    [Index(IsDescending = true)] DateTimeOffset CreatedAt);
```
