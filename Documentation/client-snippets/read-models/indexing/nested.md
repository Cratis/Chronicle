```csharp
using Cratis.Chronicle.ReadModels;

public record ReadModelsIndexingOrderLine(
    [Index] Guid ProductId,
    int Quantity);

[ReadModel]
public record ReadModelsIndexingOrderWithLines(
    Guid Id,
    IEnumerable<ReadModelsIndexingOrderLine> Lines);
```
