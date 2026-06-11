```csharp
public record Order(
    [Key] Guid Id,

    [ChildrenFrom<LineItemAdded>(key: nameof(LineItemAdded.ItemId))]
    [RemovedWith<LineItemRemoved>(key: nameof(LineItemRemoved.ItemId))]
    IEnumerable<OrderLine> Lines);

public record OrderLine(
    [Key] Guid Id,
    string Description);
```
