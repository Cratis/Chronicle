```csharp
public class DecSetPropsCombinedAccountProjection : IProjectionFor<DecSetPropsAccount>
{
    public void Define(IProjectionBuilderFor<DecSetPropsAccount> builder) => builder
        .AutoMap()  // Automatically maps matching properties
        .From<DecSetPropsAccountOpened>(_ => _
            .Set(m => m.CustomerName).To(e => e.Owner.Name)  // Custom mapping for nested property
            .Set(m => m.IsActive).ToValue(true))             // Custom mapping for constant
        .From<DecSetPropsMoneyDeposited>();  // Uses AutoMap for all properties
}
```
