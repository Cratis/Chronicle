```csharp
using Cratis.Chronicle.Projections;

public class DecVariantBacklogItemProjection : IProjectionFor<DecVariantBacklogItem>
{
    public void Define(IProjectionBuilderFor<DecVariantBacklogItem> builder) => builder
        .VariantOf<DecVariantWorkItem>(_ => _.Id)
        .EntersOn<DecVariantIssueCreated>();
}

public class DecVariantPullRequestItemProjection : IProjectionFor<DecVariantPullRequestItem>
{
    public void Define(IProjectionBuilderFor<DecVariantPullRequestItem> builder) => builder
        .VariantOf<DecVariantWorkItem>(_ => _.Id)
        .EntersOn<DecVariantPullRequestCreated>();
}
```
