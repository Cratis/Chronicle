```csharp
[EventType]
public record MbChildrenRemovalClassLineItemAdded(Guid ItemId, string Description);

[EventType]
public record MbChildrenRemovalClassLineItemRemoved(Guid OrderId, Guid ItemId);

public record MbChildrenRemovalClassOrder(
    [Key] Guid Id,

    [ChildrenFrom<MbChildrenRemovalClassLineItemAdded>(key: nameof(MbChildrenRemovalClassLineItemAdded.ItemId))]
    IEnumerable<MbChildrenRemovalClassOrderLine> Lines);

[RemovedWith<MbChildrenRemovalClassLineItemRemoved>(
    key: nameof(MbChildrenRemovalClassLineItemRemoved.ItemId),
    parentKey: nameof(MbChildrenRemovalClassLineItemRemoved.OrderId))]
public record MbChildrenRemovalClassOrderLine(
    [Key] Guid Id,
    string Description);
```
