```csharp
using System;
using System.Collections.Generic;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;

[EventType]
public record Chr0044AdvisorNamed([property: PII] string DisplayName);

public record Chr0044AdvisorSummary([property: Key] Guid AdvisorId, string DisplayName);

public record Chr0044AdvisorBook(
    [property: Key] Guid Id,
    IEnumerable<Chr0044AdvisorSummary> Advisors);

public class Chr0044AdvisorBookProjection : IProjectionFor<Chr0044AdvisorBook>
{
    public void Define(IProjectionBuilderFor<Chr0044AdvisorBook> builder) => builder
        .Children(m => m.Advisors, children => children
            .IdentifiedBy(m => m.AdvisorId)
            // Warning CHR0044: the callbackless child join is valid and AutoMaps DisplayName,
            // but IdentifiedBy cannot prove the joined event subject equals the containing document subject.
            .Join<Chr0044AdvisorNamed>());
}
```
