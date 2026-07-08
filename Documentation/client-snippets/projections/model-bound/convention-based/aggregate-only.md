```csharp title="Aggregating an event does not map its other properties"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record AggOnlyArrangementSet(string Location);

[EventType]
public record AggOnlyCandidateSubmitted(string Name, string Location);

[FromEvent<AggOnlyArrangementSet>]
public record AggOnlyAssignmentSummary(
    [Key]
    Guid Id,

    // AggOnlyCandidateSubmitted is subscribed only to be counted, so its identically named
    // Location is not auto-mapped over the value sourced from AggOnlyArrangementSet.
    [SetFrom<AggOnlyArrangementSet>(nameof(AggOnlyArrangementSet.Location))]
    string Location,

    [Count<AggOnlyCandidateSubmitted>]
    int CandidateCount);
```
