```csharp
using Cratis.Chronicle.Projections;

public class DecPassiveUserSummaryProjection : IProjectionFor<DecPassiveUserSummary>
{
    public void Define(IProjectionBuilderFor<DecPassiveUserSummary> builder) => builder
        .Passive()
        .AutoMap()
        .From<DecPassiveUserCreated>()
        .From<DecPassiveUserUpdated>();
}
```
