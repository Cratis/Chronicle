```csharp
[EventType]
public record MbJoinsMultipleOrderPlaced(Guid CustomerId);

[EventType]
public record MbJoinsMultipleCustomerCreated(string Name);

[EventType]
public record MbJoinsCustomerUpdated(string Email);

[EventType]
public record MbJoinsShippingAddressSet(string Address);

public record MbJoinsEnrichedOrder(
    [Key]
    Guid OrderId,

    [SetFrom<MbJoinsMultipleOrderPlaced>]
    Guid CustomerId,

    [Join<MbJoinsMultipleCustomerCreated>(on: nameof(CustomerId))]
    string CustomerName,

    [Join<MbJoinsCustomerUpdated>(on: nameof(CustomerId))]
    string CustomerEmail,

    // ShippingAddressSet is raised on the order's own event source, so it joins on the
    // read model's own key rather than a separate correlating property.
    [Join<MbJoinsShippingAddressSet>(on: nameof(OrderId))]
    string ShippingAddress);
```
