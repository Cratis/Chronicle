```csharp
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

// Illustrative subset of Cratis.Chronicle.Events.EventContext's real shape
public record EventProcessingEventContextShape(
    EventSequenceNumber SequenceNumber,
    EventSourceId EventSourceId,
    EventType EventType,
    DateTimeOffset Occurred,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    Identity CausedBy);
// ... and more — see EventContext for the full member list
```
