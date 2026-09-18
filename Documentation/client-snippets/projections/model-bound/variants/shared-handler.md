```csharp
[EventType]
public record MbVariantSharedIssueCreated(string Title);

[EventType]
public record MbVariantSharedPullRequestCreated(string PullRequestUrl);

[EventType]
public record MbVariantSharedTitleChanged(string Title);

public class MbVariantSharedWorkItem;

[VariantOf<MbVariantSharedWorkItem>]
[EntersOn<MbVariantSharedIssueCreated>]
public record MbVariantSharedBacklogItem([property: Key] Guid Id, string Title);

[VariantOf<MbVariantSharedWorkItem>]
[EntersOn<MbVariantSharedPullRequestCreated>]
public record MbVariantSharedPullRequestItem(
    [property: Key] Guid Id,
    string Title,
    [property: SetFrom<MbVariantSharedPullRequestCreated>] string PullRequestUrl);

/// <summary>
/// Declares a mapping every variant of MbVariantSharedWorkItem shares. Every variant must have a
/// Title member - one that does not is a declaration error, not a silently skipped mapping.
/// </summary>
/// <param name="Title">The title every variant carrying one keeps up to date.</param>
[GlobalFor<MbVariantSharedWorkItem>]
public record MbVariantSharedHandlers([property: SetFrom<MbVariantSharedTitleChanged>] string Title);
```
