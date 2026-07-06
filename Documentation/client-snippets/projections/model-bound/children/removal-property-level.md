```csharp
[EventType]
public record MbChildrenRemovalPropertyLineItemAdded(Guid ItemId, string Description);

[EventType]
public record MbChildrenRemovalPropertyLineItemRemoved(Guid ItemId);

public record MbChildrenRemovalPropertyOrder(
    [Key] Guid Id,

    [ChildrenFrom<MbChildrenRemovalPropertyLineItemAdded>(key: nameof(MbChildrenRemovalPropertyLineItemAdded.ItemId))]
    [RemovedWith<MbChildrenRemovalPropertyLineItemRemoved>(key: nameof(MbChildrenRemovalPropertyLineItemRemoved.ItemId))]
    IEnumerable<MbChildrenRemovalPropertyOrderLine> Lines);

public record MbChildrenRemovalPropertyOrderLine(
    [Key] Guid Id,
    string Description);
```
