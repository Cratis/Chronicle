```csharp title="Aggregating an event does not map its other properties"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DeclAggArrangementSet(string Location);

[EventType]
public record DeclAggCandidateSubmitted(string Name, string Location);

public record DeclAggAssignmentSummary(string Location, int CandidateCount);

public class DeclAggAssignmentProjection : IProjectionFor<DeclAggAssignmentSummary>
{
    public void Define(IProjectionBuilderFor<DeclAggAssignmentSummary> builder) => builder
        .From<DeclAggArrangementSet>()
        .From<DeclAggCandidateSubmitted>(_ => _
            .Count(m => m.CandidateCount));
}
```
