```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record MbIndexExplicitMbAccountInfo(
    [Key] Guid Id,
    [SetFrom<MbIndexExplicitAccountOpened>(nameof(MbIndexExplicitAccountOpened.Name))] string Name,
    [SetFrom<MbIndexExplicitAccountOpened>(nameof(MbIndexExplicitAccountOpened.InitialBalance))] decimal Balance);
```
