```csharp
// Events
[EventType]
public record MbConstantKeyProductPurchased(string ProductId, decimal Amount);

[EventType]
public record MbConstantKeyProductReturned(string ProductId, decimal Amount);

[EventType]
public record MbConstantKeyPageViewed(string PageUrl);

// Global read model
public record MbConstantKeyStoreMetrics(
    [Count<MbConstantKeyProductPurchased>(ConstantKey = "store")]
    int TotalPurchases,

    [Count<MbConstantKeyProductReturned>(ConstantKey = "store")]
    int TotalReturns,

    [Increment<MbConstantKeyProductPurchased>(ConstantKey = "store")]
    [Decrement<MbConstantKeyProductReturned>(ConstantKey = "store")]
    int NetTransactions,

    [Count<MbConstantKeyPageViewed>(ConstantKey = "store")]
    int TotalPageViews);
```
