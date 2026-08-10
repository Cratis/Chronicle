```csharp
using System;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;

[EventType]
public record Chr0043AdvisorNamed(Guid RequestId, [property: PII] string FullName);

public record Chr0043RequestSummary([property: Key] Guid Id, string AdvisorName);

public class Chr0043RequestSummaryProjection : IProjectionFor<Chr0043RequestSummary>
{
    public void Define(IProjectionBuilderFor<Chr0043RequestSummary> builder) => builder
        .From<Chr0043AdvisorNamed>(_ => _
            // Warning CHR0043: the resolved document is routed through RequestId while
            // FullName belongs to the persisted event subject, which may be another identity.
            .UsingKey(e => e.RequestId)
            .Set(m => m.AdvisorName).To(e => e.FullName));
}
```
