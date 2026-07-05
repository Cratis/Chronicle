```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record ArchitectureDeclarativeItemAdded(string Category);

public record ArchitectureDeclarativeSummary(
    string Category,
    int Count);

public class ArchitectureDeclarativeSummaryProjection : IProjectionFor<ArchitectureDeclarativeSummary>
{
    public void Define(IProjectionBuilderFor<ArchitectureDeclarativeSummary> builder) => builder
        .From<ArchitectureDeclarativeItemAdded>(_ => _
            .UsingKey(e => e.Category)
            .Count(m => m.Count));
}
```
