```csharp
using Cratis.Chronicle.Projections;

[EventType]
public record MbChildrenNoAutoMapLineItemAdded(
    Guid ItemId,
    string ProductName,
    int Quantity,
    decimal Price);

public record MbChildrenNoAutoMapOrder(
    [Key]
    Guid OrderId,

    [ChildrenFrom<MbChildrenNoAutoMapLineItemAdded>(key: nameof(MbChildrenNoAutoMapLineItemAdded.ItemId))]
    IEnumerable<MbChildrenNoAutoMapLineItem> Items);

[NoAutoMap]
public record MbChildrenNoAutoMapLineItem(
    [Key] Guid Id,

    // Now you must use SetFrom for each property
    [SetFrom<MbChildrenNoAutoMapLineItemAdded>(nameof(MbChildrenNoAutoMapLineItemAdded.ProductName))]
    string ProductName,

    [SetFrom<MbChildrenNoAutoMapLineItemAdded>(nameof(MbChildrenNoAutoMapLineItemAdded.Quantity))]
    int Quantity,

    [SetFrom<MbChildrenNoAutoMapLineItemAdded>(nameof(MbChildrenNoAutoMapLineItemAdded.Price))]
    decimal Price);
```
