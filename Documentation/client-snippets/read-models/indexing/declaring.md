```csharp
using Cratis.Chronicle.ReadModels;

[ReadModel]
public record ReadModelsIndexingOrder(
    Guid Id,
    [Index] Guid CustomerId,
    [Index] string Number,
    decimal Total);
```
