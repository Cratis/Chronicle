```csharp
[EventType]
public record MbChildrenAutoMapLineItemAdded(
    Guid ItemId,
    string ProductName,
    int Quantity,
    decimal Price);

public record MbChildrenAutoMapOrder(
    [Key]
    Guid OrderId,

    [ChildrenFrom<MbChildrenAutoMapLineItemAdded>(key: nameof(MbChildrenAutoMapLineItemAdded.ItemId))]
    IEnumerable<MbChildrenAutoMapLineItem> Items);

public record MbChildrenAutoMapLineItem(
    [Key] Guid Id,
    string ProductName,  // Automatically mapped from MbChildrenAutoMapLineItemAdded.ProductName
    int Quantity,        // Automatically mapped from MbChildrenAutoMapLineItemAdded.Quantity
    decimal Price);      // Automatically mapped from MbChildrenAutoMapLineItemAdded.Price
```
