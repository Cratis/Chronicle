```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbVariantFullIssueCreated(string Title);

[EventType]
public record MbVariantFullPullRequestCreated(string PullRequestUrl);

[EventType]
public record MbVariantFullBuildCompleted(string BuildStatus);

[EventType]
public record MbVariantFullTitleChanged(string Title);

/// <summary>
/// Anchors the logical identity shared by MbVariantFullBacklogItem and MbVariantFullPullRequestItem.
/// Deliberately not a read model itself.
/// </summary>
public class MbVariantFullWorkItem;

/// <summary>
/// The variant an entity is in before a pull request exists for it.
/// </summary>
[VariantOf<MbVariantFullWorkItem>]
[EntersOn<MbVariantFullIssueCreated>]
public record MbVariantFullBacklogItem([property: Key] Guid Id, string Title);

/// <summary>
/// The variant an entity enters once a pull request is created for it. BuildStatus is
/// mapped from MbVariantFullBuildCompleted - an event that is NOT this variant's entering event, so it
/// becomes an update-only join and can never create the row on its own.
/// </summary>
[VariantOf<MbVariantFullWorkItem>]
[EntersOn<MbVariantFullPullRequestCreated>]
public record MbVariantFullPullRequestItem(
    [property: Key] Guid Id,
    string Title,
    [property: SetFrom<MbVariantFullPullRequestCreated>] string PullRequestUrl,
    [property: SetFrom<MbVariantFullBuildCompleted>] string BuildStatus);

/// <summary>
/// Declares a mapping every variant of MbVariantFullWorkItem shares.
/// </summary>
/// <param name="Title">The title every variant carrying one keeps up to date.</param>
[GlobalFor<MbVariantFullWorkItem>]
public record MbVariantFullSharedHandlers([property: SetFrom<MbVariantFullTitleChanged>] string Title);
```
