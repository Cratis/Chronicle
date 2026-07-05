```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecFromEventSequencePackageCreated(string PackageId);

[EventType]
public record DecFromEventSequencePackageShipped(string PackageId, DateTimeOffset ShippedAt);

[EventType]
public record DecFromEventSequencePackageDelivered(string PackageId, DateTimeOffset DeliveredAt);

public record DecFromEventSequenceShipping(
    string PackageId,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? DeliveredAt);

// Projection for order management events
public class DecFromEventSequenceMultiOrderProjection : IProjectionFor<DecFromEventSequenceOrder>
{
    public void Define(IProjectionBuilderFor<DecFromEventSequenceOrder> builder) => builder
        .FromEventSequence("order-management")
        .AutoMap()
        .From<DecFromEventSequenceOrderCreated>(_ => _
            .Set(m => m.Status).ToValue(DecFromEventSequenceOrderStatus.Created));
}

// Projection for shipping events from a different sequence
public class DecFromEventSequenceShippingProjection : IProjectionFor<DecFromEventSequenceShipping>
{
    public void Define(IProjectionBuilderFor<DecFromEventSequenceShipping> builder) => builder
        .FromEventSequence("shipping-management")
        .AutoMap()
        .From<DecFromEventSequencePackageCreated>()
        .From<DecFromEventSequencePackageShipped>()
        .From<DecFromEventSequencePackageDelivered>();
}
```
