```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecVariantUpdatingPullRequestCreated(string PullRequestUrl);

[EventType]
public record DecVariantUpdatingBuildCompleted(string BuildStatus);

public class DecVariantUpdatingWorkItem;

public record DecVariantUpdatingPullRequestItem(Guid Id, string PullRequestUrl, string BuildStatus);

/// <summary>
/// From&lt;DecVariantUpdatingBuildCompleted&gt; is declared exactly like an ordinary multi-event
/// projection. Because that event is NOT the one named with EntersOn, the builder automatically
/// reclassifies it into an update-only join on the variant's own key when the definition is built - it
/// can bring an already-active instance up to date, but it can never create one on its own.
/// </summary>
public class DecVariantUpdatingPullRequestItemProjection : IProjectionFor<DecVariantUpdatingPullRequestItem>
{
    public void Define(IProjectionBuilderFor<DecVariantUpdatingPullRequestItem> builder) => builder
        .VariantOf<DecVariantUpdatingWorkItem>(_ => _.Id)
        .EntersOn<DecVariantUpdatingPullRequestCreated>()
        .From<DecVariantUpdatingBuildCompleted>();
}
```
