```csharp
using Cratis.Chronicle.Projections;

public class DecPassiveUserSummaryProjection : IProjectionFor<DecPassiveUserSummary>
{
    public void Define(IProjectionBuilderFor<DecPassiveUserSummary> builder) => builder
        .Passive()
        .From<DecPassiveUserCreated>()
        .From<DecPassiveUserUpdated>();
}
```
