```csharp
/// <summary>
/// Anchors the logical identity shared by DecVariantBacklogItem and DecVariantPullRequestItem.
/// Deliberately not a read model itself, and does not need a common CLR base type with either variant.
/// </summary>
public class DecVariantWorkItem;

public record DecVariantBacklogItem(Guid Id, string Title);

public record DecVariantPullRequestItem(Guid Id, string PullRequestUrl);
```
