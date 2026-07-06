```csharp
// Events
[EventType]
public record MbRemovalFullShoppingCartCreated(string CustomerName);

[EventType]
public record MbRemovalFullItemAddedToCart(Guid ItemId, string ProductName, decimal Price);

[EventType]
public record MbRemovalFullItemRemovedFromCart(Guid CartId, Guid ItemId);

[EventType]
public record MbRemovalFullCartCheckedOut;

[EventType]
public record MbRemovalFullCartAbandoned;

// Read Models
[RemovedWith<MbRemovalFullCartCheckedOut>]
[RemovedWith<MbRemovalFullCartAbandoned>]
public record MbRemovalFullShoppingCart(
    [Key]
    Guid Id,

    [SetFrom<MbRemovalFullShoppingCartCreated>(nameof(MbRemovalFullShoppingCartCreated.CustomerName))]
    string Customer,

    [ChildrenFrom<MbRemovalFullItemAddedToCart>(key: nameof(MbRemovalFullItemAddedToCart.ItemId))]
    IEnumerable<MbRemovalFullCartItem> Items);

[RemovedWith<MbRemovalFullItemRemovedFromCart>(
    key: nameof(MbRemovalFullItemRemovedFromCart.ItemId),
    parentKey: nameof(MbRemovalFullItemRemovedFromCart.CartId))]
public record MbRemovalFullCartItem(
    [Key] Guid Id,

    [SetFrom<MbRemovalFullItemAddedToCart>(nameof(MbRemovalFullItemAddedToCart.ProductName))]
    string Product,

    [SetFrom<MbRemovalFullItemAddedToCart>(nameof(MbRemovalFullItemAddedToCart.Price))]
    decimal Price);
```
