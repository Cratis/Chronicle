```csharp title="Matching nested structures and collections"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record ConventionAddress(string Street, string City, string PostalCode);

public record ConventionLineItem(string ProductName, decimal UnitPrice, int Quantity);

[EventType]
public record ConventionCustomerRegistered(
    string FirstName,
    string LastName,
    ConventionAddress BillingAddress,
    ConventionAddress ShippingAddress);

[EventType]
public record ConventionOrderCreated(
    string CustomerEmail,
    ConventionLineItem[] Items,
    string[] Tags);

[FromEvent<ConventionCustomerRegistered>]
public record ConventionCustomer(
    [Key] Guid Id,
    string FirstName,
    string LastName,
    ConventionAddress BillingAddress,
    ConventionAddress ShippingAddress);

[FromEvent<ConventionOrderCreated>]
public record ConventionOrder(
    [Key] Guid Id,
    string CustomerEmail,
    ConventionLineItem[] Items,
    string[] Tags);
```
