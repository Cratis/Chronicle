```csharp
// Events
[EventType]
public record MbChildrenFullOrderCreated(string CustomerName);

[EventType]
public record MbChildrenFullLineItemAdded(
    Guid ItemId,
    string ProductName,
    int InitialQuantity,
    decimal UnitPrice);

[EventType]
public record MbChildrenFullQuantityAdjusted(Guid ItemId, int NewQuantity);

[EventType]
public record MbChildrenFullLineItemRemoved(Guid ItemId);

// Read Models
public record MbChildrenFullOrder(
    [Key]
    Guid Id,

    [SetFrom<MbChildrenFullOrderCreated>(nameof(MbChildrenFullOrderCreated.CustomerName))]
    string Customer,

    [ChildrenFrom<MbChildrenFullLineItemAdded>(key: nameof(MbChildrenFullLineItemAdded.ItemId))]
    [RemovedWith<MbChildrenFullLineItemRemoved>(key: nameof(MbChildrenFullLineItemRemoved.ItemId))]
    IEnumerable<MbChildrenFullOrderLine> Lines);

public record MbChildrenFullOrderLine(
    [Key] Guid Id,

    [SetFrom<MbChildrenFullLineItemAdded>(nameof(MbChildrenFullLineItemAdded.ProductName))]
    string Product,

    [SetFrom<MbChildrenFullLineItemAdded>(nameof(MbChildrenFullLineItemAdded.InitialQuantity))]
    [SetFrom<MbChildrenFullQuantityAdjusted>(nameof(MbChildrenFullQuantityAdjusted.NewQuantity))]
    int Quantity,

    [SetFrom<MbChildrenFullLineItemAdded>(nameof(MbChildrenFullLineItemAdded.UnitPrice))]
    decimal UnitPrice);
```
