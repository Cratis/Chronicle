```csharp title="Reuse one address type with different mappings"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record ScopedAddressOrderPlaced(string CustomerName);

[EventType]
public record ScopedAddressProductRegistered(string Name);

public record ScopedAddress(string Name);
public record ScopedAddressOrder(ScopedAddress? BillingAddress, ScopedAddress? ShippingAddress);

public class ScopedAddressOrderProjection : IProjectionFor<ScopedAddressOrder>
{
    public void Define(IProjectionBuilderFor<ScopedAddressOrder> builder) => builder
        .From<ScopedAddressOrderPlaced>()
        .Nested(m => m.BillingAddress, nested => nested
            .From<ScopedAddressOrderPlaced>(mapping => mapping
                .Set(address => address.Name).To(e => e.CustomerName)))
        .Nested(m => m.ShippingAddress, nested => nested
            .From<ScopedAddressProductRegistered>(mapping => mapping
                .Set(address => address.Name).To(e => e.Name)));
}
```
