```csharp
[EventType]
public record PullRequestRecorded(string Owner, string Repository, int Number, string Title);

[EventType]
public record PullRequestRemoved(string Owner, string Repository, int Number);

public record PullRequestKey(string Owner, string Repository, int Number);

public record TrackedPullRequest([Key] PullRequestKey Id, string Title);

public record PullRequestDashboard(
    [Key] Guid Id,
    [ChildrenFrom<PullRequestRecorded>(CompositeKey = new[]
    {
        nameof(PullRequestRecorded.Owner), nameof(PullRequestRecorded.Repository), nameof(PullRequestRecorded.Number)
    })]
    [RemovedWith<PullRequestRemoved>(CompositeKey = new[]
    {
        nameof(PullRequestRemoved.Owner), nameof(PullRequestRemoved.Repository), nameof(PullRequestRemoved.Number)
    })]
    IEnumerable<TrackedPullRequest> PullRequests);
```
