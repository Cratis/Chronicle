```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbConstantKeyOrderPlacedForMetrics;

[EventType]
public record MbConstantKeyUserLoggedIn;

[EventType]
public record MbConstantKeyUserLoggedOut;

[EventType]
public record MbConstantKeyErrorOccurred;

public record MbConstantKeySystemMetrics(
    [Count<MbConstantKeyOrderPlacedForMetrics>(ConstantKey = "metrics")]
    int TotalOrders,

    [Increment<MbConstantKeyUserLoggedIn>(ConstantKey = "metrics")]
    [Decrement<MbConstantKeyUserLoggedOut>(ConstantKey = "metrics")]
    int ActiveSessions,

    [Count<MbConstantKeyErrorOccurred>(ConstantKey = "metrics")]
    int TotalErrors);
```
