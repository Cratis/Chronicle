```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecVariantFullIssueCreated(string Title);

[EventType]
public record DecVariantFullPullRequestCreated(string PullRequestUrl);

[EventType]
public record DecVariantFullBuildCompleted(string BuildStatus);

/// <summary>
/// Anchors the logical identity shared by DecVariantFullBacklogItem and
/// DecVariantFullPullRequestItem. Deliberately not a read model itself.
/// </summary>
public class DecVariantFullWorkItem;

public record DecVariantFullBacklogItem(Guid Id, string Title);

public record DecVariantFullPullRequestItem(Guid Id, string PullRequestUrl, string BuildStatus);

/// <summary>
/// The variant an entity is in before a pull request exists for it.
/// </summary>
public class DecVariantFullBacklogItemProjection : IProjectionFor<DecVariantFullBacklogItem>
{
    public void Define(IProjectionBuilderFor<DecVariantFullBacklogItem> builder) => builder
        .VariantOf<DecVariantFullWorkItem>(_ => _.Id)
        .EntersOn<DecVariantFullIssueCreated>();
}

/// <summary>
/// The variant an entity enters once a pull request is created for it. BuildStatus comes from
/// DecVariantFullBuildCompleted - an event that is NOT this variant's entering event, so the builder
/// reclassifies it into an update-only join and it can never create the row on its own.
/// </summary>
public class DecVariantFullPullRequestItemProjection : IProjectionFor<DecVariantFullPullRequestItem>
{
    public void Define(IProjectionBuilderFor<DecVariantFullPullRequestItem> builder) => builder
        .VariantOf<DecVariantFullWorkItem>(_ => _.Id)
        .EntersOn<DecVariantFullPullRequestCreated>()
        .From<DecVariantFullBuildCompleted>();
}
```
