```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbVariantIssueCreated(string Title);

[EventType]
public record MbVariantPullRequestCreated(string PullRequestUrl);

/// <summary>
/// Anchors the logical identity shared by every variant. It does not need to be a
/// read model itself, and it does not need a common CLR base type with any of the variants.
/// </summary>
public class MbVariantWorkItem;

[VariantOf<MbVariantWorkItem>]
[EntersOn<MbVariantIssueCreated>]
public record MbVariantBacklogItem([property: Key] Guid Id, string Title);

[VariantOf<MbVariantWorkItem>]
[EntersOn<MbVariantPullRequestCreated>]
public record MbVariantPullRequestItem([property: Key] Guid Id, [property: SetFrom<MbVariantPullRequestCreated>] string PullRequestUrl);
```
