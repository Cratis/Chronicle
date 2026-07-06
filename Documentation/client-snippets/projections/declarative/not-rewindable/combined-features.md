```csharp
using Cratis.Chronicle.Projections;

[EventType]
public record DecNotRewindableOrderReceived(string OrderId);

[EventType]
public record DecNotRewindableOrderProcessing(string OrderId);

[EventType]
public record DecNotRewindableOrderCompleted(string OrderId);

public record DecNotRewindableOrderStatus(
    string Status,
    DateTimeOffset LastUpdatedAt);

public class DecNotRewindableRealTimeOrderStatusProjection : IProjectionFor<DecNotRewindableOrderStatus>
{
    public void Define(IProjectionBuilderFor<DecNotRewindableOrderStatus> builder) => builder
        .NotRewindable()
        .FromEventSequence("order-processing")
        .Passive()
        .AutoMap()
        .FromEvery(_ => _
            .Set(m => m.LastUpdatedAt).ToEventContextProperty(c => c.Occurred))
        .From<DecNotRewindableOrderReceived>(_ => _
            .Set(m => m.Status).ToValue("RECEIVED"))
        .From<DecNotRewindableOrderProcessing>(_ => _
            .Set(m => m.Status).ToValue("PROCESSING"))
        .From<DecNotRewindableOrderCompleted>(_ => _
            .Set(m => m.Status).ToValue("COMPLETED"));
}
```
