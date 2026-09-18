```csharp
[EventType]
public record MbVariantUpdatingPullRequestCreated(string PullRequestUrl);

[EventType]
public record MbVariantUpdatingBuildCompleted(string BuildStatus);

public class MbVariantUpdatingWorkItem;

/// <summary>
/// BuildStatus is mapped from MbVariantUpdatingBuildCompleted - an event that is NOT this
/// variant's entering event, so it is automatically reclassified into an update-only join. It can bring
/// an already-active instance up to date, but it can never create one on its own.
/// </summary>
[VariantOf<MbVariantUpdatingWorkItem>]
[EntersOn<MbVariantUpdatingPullRequestCreated>]
public record MbVariantUpdatingPullRequestItem(
    [property: Key] Guid Id,
    [property: SetFrom<MbVariantUpdatingPullRequestCreated>] string PullRequestUrl,
    [property: SetFrom<MbVariantUpdatingBuildCompleted>] string BuildStatus);
```
