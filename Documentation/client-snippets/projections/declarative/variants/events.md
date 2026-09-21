```csharp
using Cratis.Chronicle.Events;

[EventType]
public record DecVariantIssueCreated(string Title);

[EventType]
public record DecVariantPullRequestCreated(string PullRequestUrl);
```
