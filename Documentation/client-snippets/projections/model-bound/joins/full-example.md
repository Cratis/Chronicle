```csharp
// Events
[EventType]
public record MbJoinsFullOrderPlaced(Guid CustomerId, DateTimeOffset PlacedAt);

[EventType]
public record MbJoinsFullCustomerRegistered(string Name, string Email);

[EventType]
public record MbJoinsFullCustomerProfileUpdated(string PhoneNumber);

[EventType]
public record MbJoinsFullLineItemAdded(Guid ProductId, int Quantity);

[EventType]
public record MbJoinsFullProductCreated(string Name, decimal Price);

[EventType]
public record MbJoinsFullProductPriceChanged(decimal NewPrice);

// Read Models
public record MbJoinsFullOrderDetails(
    [Key]
    Guid OrderId,

    [SetFrom<MbJoinsFullOrderPlaced>]
    DateTimeOffset PlacedAt,

    [SetFrom<MbJoinsFullOrderPlaced>]
    Guid CustomerId,

    // Join customer information
    [Join<MbJoinsFullCustomerRegistered>(
        on: nameof(CustomerId),
        eventPropertyName: nameof(MbJoinsFullCustomerRegistered.Name))]
    string CustomerName,

    [Join<MbJoinsFullCustomerRegistered>(
        on: nameof(CustomerId),
        eventPropertyName: nameof(MbJoinsFullCustomerRegistered.Email))]
    string CustomerEmail,

    [Join<MbJoinsFullCustomerProfileUpdated>(
        on: nameof(CustomerId),
        eventPropertyName: nameof(MbJoinsFullCustomerProfileUpdated.PhoneNumber))]
    string CustomerPhone,

    [ChildrenFrom<MbJoinsFullLineItemAdded>(key: nameof(MbJoinsFullLineItemAdded.ProductId))]
    IEnumerable<MbJoinsFullLineItemDetails> Items);

// Keyed by product id, so the joins below resolve implicitly through the child's own key.
public record MbJoinsFullLineItemDetails(
    [Key] Guid ProductId,

    [SetFrom<MbJoinsFullLineItemAdded>]
    int Quantity,

    // Join product information
    [Join<MbJoinsFullProductCreated>(eventPropertyName: nameof(MbJoinsFullProductCreated.Name))]
    string ProductName,

    [Join<MbJoinsFullProductCreated>(eventPropertyName: nameof(MbJoinsFullProductCreated.Price))]
    [Join<MbJoinsFullProductPriceChanged>(eventPropertyName: nameof(MbJoinsFullProductPriceChanged.NewPrice))]
    decimal Price);
```
