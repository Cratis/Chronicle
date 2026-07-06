```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbChildrenLineItemAdded(
    Guid ItemId,
    string ProductName,
    int Quantity,
    decimal Price);

public record MbChildrenOrder(
    [Key]
    Guid OrderId,

    [ChildrenFrom<MbChildrenLineItemAdded>(key: nameof(MbChildrenLineItemAdded.ItemId))]
    IEnumerable<MbChildrenLineItem> Items);

public record MbChildrenLineItem(
    [Key] Guid Id,  // Chronicle automatically discovers this as the key
    string ProductName,
    int Quantity,
    decimal Price);
```
