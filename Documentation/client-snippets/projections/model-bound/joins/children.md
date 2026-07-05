```csharp
[EventType]
public record MbJoinsLineItemAdded(Guid ProductId, int Quantity);

[EventType]
public record MbJoinsProductUpdated(string ProductName, decimal CurrentPrice);

public record MbJoinsOrder(
    [Key]
    Guid OrderId,

    [ChildrenFrom<MbJoinsLineItemAdded>(key: nameof(MbJoinsLineItemAdded.ProductId))]
    IEnumerable<MbJoinsOrderLine> Lines);

// The line's key is the product id, so the join to ProductUpdated (raised on that
// same product's event source) resolves implicitly through the child's own key.
public record MbJoinsOrderLine(
    [Key] Guid ProductId,

    [SetFrom<MbJoinsLineItemAdded>]
    int Quantity,

    [Join<MbJoinsProductUpdated>(eventPropertyName: nameof(MbJoinsProductUpdated.ProductName))]
    string ProductName,

    [Join<MbJoinsProductUpdated>(eventPropertyName: nameof(MbJoinsProductUpdated.CurrentPrice))]
    decimal Price);
```
