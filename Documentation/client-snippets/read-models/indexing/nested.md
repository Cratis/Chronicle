```csharp
[ReadModel]
public record IndexingOrderWithLines(
    Guid Id,
    IEnumerable<IndexingOrderLine> Lines);

public record IndexingOrderLine(
    [Index] Guid ProductId,
    int Quantity);
```
