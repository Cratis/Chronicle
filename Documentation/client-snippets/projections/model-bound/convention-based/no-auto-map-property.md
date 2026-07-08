```csharp title="Exclude a single property from convention mapping"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record NoAutoMapWorkArrangementSet(string Location, int WorkMode);

[EventType]
public record NoAutoMapCandidateSubmitted(string Name, string Location);

[FromEvent<NoAutoMapWorkArrangementSet>]
public record NoAutoMapAssignmentSummary(
    [Key]
    Guid Id,

    // Location is sourced only from NoAutoMapWorkArrangementSet. NoAutoMapCandidateSubmitted is
    // value-mapped (for CandidateName) and also carries a Location; [NoAutoMap] stops that Location
    // from being auto-mapped over the explicit value, while every other property keeps mapping.
    [SetFrom<NoAutoMapWorkArrangementSet>(nameof(NoAutoMapWorkArrangementSet.Location))]
    [NoAutoMap]
    string Location,

    [SetFrom<NoAutoMapCandidateSubmitted>(nameof(NoAutoMapCandidateSubmitted.Name))]
    string CandidateName);
```
