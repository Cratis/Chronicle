```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record EmptyChildrenOrderPlaced(string Customer);

[EventType]
public record EmptyChildrenLineItemAdded(Guid ItemId, string ProductName, int Quantity);

public record EmptyChildrenLineItem([Key] Guid Id, string ProductName, int Quantity);

// Non-nullable: an order with no line items reads back as an empty collection,
// so enumerating Lines never needs a guard.
[FromEvent<EmptyChildrenOrderPlaced>]
public record EmptyChildrenOrder(
    [Key]
    Guid OrderId,

    string Customer,

    [ChildrenFrom<EmptyChildrenLineItemAdded>(key: nameof(EmptyChildrenLineItemAdded.ItemId))]
    IEnumerable<EmptyChildrenLineItem> Lines);

// Nullable: "no line items yet" stays distinguishable from "an empty list".
[FromEvent<EmptyChildrenOrderPlaced>]
public record EmptyChildrenDraftOrder(
    [Key]
    Guid DraftOrderId,

    string Customer,

    [ChildrenFrom<EmptyChildrenLineItemAdded>(key: nameof(EmptyChildrenLineItemAdded.ItemId))]
    IEnumerable<EmptyChildrenLineItem>? Lines);
```
