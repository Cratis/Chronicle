```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record ComplianceReadModelsPatientAdmitted(ComplianceReadModelsPersonName Name, DateTimeOffset AdmittedAt);

public record ComplianceReadModelsPatientSummary(Guid PatientId, [PII] string Name, DateTimeOffset LastAdmittedAt);

public class ComplianceReadModelsPatientSummaryReducer : IReducerFor<ComplianceReadModelsPatientSummary>
{
    public ComplianceReadModelsPatientSummary Admitted(ComplianceReadModelsPatientAdmitted @event, ComplianceReadModelsPatientSummary? current, EventContext context) =>
        new(
            Guid.Parse(context.EventSourceId.Value),
            @event.Name,
            @event.AdmittedAt);
}
```
