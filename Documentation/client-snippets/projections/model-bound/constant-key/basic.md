```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbConstantKeyOrderPlaced(string CustomerName, DateTimeOffset PlacedAt);

[FromEvent<MbConstantKeyOrderPlaced>(ConstantKey = "global")]
public record MbConstantKeyGlobalOrderSummary(
    [SetFrom<MbConstantKeyOrderPlaced>(nameof(MbConstantKeyOrderPlaced.CustomerName))]
    string LastCustomer,

    [SetFrom<MbConstantKeyOrderPlaced>(nameof(MbConstantKeyOrderPlaced.PlacedAt))]
    DateTimeOffset LastOrderDate);
```
