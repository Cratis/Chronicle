```csharp
using Cratis.Chronicle.Projections;

public class DecSetPropsAccountProjection : IProjectionFor<DecSetPropsAccount>
{
    public void Define(IProjectionBuilderFor<DecSetPropsAccount> builder) => builder
        .From<DecSetPropsAccountOpened>(_ => _
            .Set(m => m.AccountNumber).To(e => e.Number)
            .Set(m => m.CustomerName).To(e => e.Owner.Name)
            .Set(m => m.Balance).ToValue(42.0m)
            .Set(m => m.IsActive).ToValue(true)
            .Set(m => m.OpenedAt).To(e => e.Timestamp))
        .From<DecSetPropsMoneyDeposited>(_ => _
            .Set(m => m.Balance).To(e => e.Amount)
            .Set(m => m.LastTransaction).To(e => e.Timestamp));
}
```
