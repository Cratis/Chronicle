```csharp
using Cratis.Chronicle.ReadModels;

[ReadModel]
public record ReadModelsIndexingOrder(
    Guid Id,
    [property: Index] Guid CustomerId,
    [property: Index] string Number,
    decimal Total);
```
