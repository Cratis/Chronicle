```csharp
[EventType]
public record MbChildrenCountersItemAddedToCart(Guid ItemId, string ProductName, decimal Price, int InitialQuantity);

[EventType]
public record MbChildrenCountersQuantityIncreased(Guid ItemId);

[EventType]
public record MbChildrenCountersQuantityDecreased(Guid ItemId);

public record MbChildrenCountersShoppingCart(
    [Key]
    Guid CartId,

    [ChildrenFrom<MbChildrenCountersItemAddedToCart>(
        key: nameof(MbChildrenCountersItemAddedToCart.ItemId))]
    IEnumerable<MbChildrenCountersCartItem> Items);

// Child type with its own projection attributes
public record MbChildrenCountersCartItem(
    [Key] Guid Id,

    [SetFrom<MbChildrenCountersItemAddedToCart>(nameof(MbChildrenCountersItemAddedToCart.ProductName))]
    string ProductName,

    [SetFrom<MbChildrenCountersItemAddedToCart>(nameof(MbChildrenCountersItemAddedToCart.Price))]
    decimal Price,

    [SetFrom<MbChildrenCountersItemAddedToCart>(nameof(MbChildrenCountersItemAddedToCart.InitialQuantity))]
    [Increment<MbChildrenCountersQuantityIncreased>]
    [Decrement<MbChildrenCountersQuantityDecreased>]
    int Quantity);
```
