```csharp
public record Order(
    [Key] Guid Id,

    [ChildrenFrom<LineItemAdded>(key: nameof(LineItemAdded.ItemId))]
    IEnumerable<OrderLine> Lines);

[RemovedWith<LineItemRemoved>(
    key: nameof(LineItemRemoved.ItemId),
    parentKey: nameof(LineItemRemoved.OrderId))]
public record OrderLine(
    [Key] Guid Id,
    string Description);
```
