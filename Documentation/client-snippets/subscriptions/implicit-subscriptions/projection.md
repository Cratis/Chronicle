```csharp
using Cratis.Chronicle.Projections;

public record SubscriptionsImplicitFulfillmentReadModel(string TrackingNumber, string Status);

public class SubscriptionsImplicitFulfillmentProjection : IProjectionFor<SubscriptionsImplicitFulfillmentReadModel>
{
    public void Define(IProjectionBuilderFor<SubscriptionsImplicitFulfillmentReadModel> builder) =>
        builder
            .From<SubscriptionsImplicitShipmentDispatched>(_ => _
                .Set(m => m.TrackingNumber).To(e => e.TrackingNumber)
                .Set(m => m.Status).ToValue("Dispatched"))
            .From<SubscriptionsImplicitShipmentFailed>(_ => _
                .Set(m => m.Status).ToValue("Failed"));
}
```
