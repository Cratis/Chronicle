```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record TaggingAuditEntryRecorded(string EntryId, string Category);

public record TaggingComplianceReport(string EntryId, string Category);

[Tag("Analytics")]
[Tag("Compliance")]
[Tag("Auditing")]
public class TaggingComplianceReportProjection : IProjectionFor<TaggingComplianceReport>
{
    public void Define(IProjectionBuilderFor<TaggingComplianceReport> builder) => builder
        .From<TaggingAuditEntryRecorded>(_ => _
            .Set(m => m.EntryId).To(e => e.EntryId)
            .Set(m => m.Category).To(e => e.Category));
}
```
