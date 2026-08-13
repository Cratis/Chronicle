```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Compliance.GDPR;

[PII]
public record ComplianceReadModelsPostponementComment(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator ComplianceReadModelsPostponementComment(string value) => new(value);
}

// The stored, person-scoped read model. Its key is the person, so the comment is encrypted under
// that person's key at rest.
public record ComplianceReadModelsRetentionSubject(
    string Id,
    ComplianceReadModelsPostponementComment Comment);

// The row a query composes in memory. Its identity is not named Id and it carries no [Subject], so
// it has no compliance subject of its own — [SubjectFrom] says which subject the lifted comment
// belongs to.
public record ComplianceReadModelsRetentionDueSubject(
    string SubjectId,
    DateTimeOffset DueAt,
    [SubjectFrom(nameof(SubjectId))] ComplianceReadModelsPostponementComment Comment);

public class ComplianceReadModelsRetentionDueService(IEventStore eventStore)
{
    public Task<ComplianceReadModelsRetentionDueSubject> Compose(
        ComplianceReadModelsRetentionSubject stored,
        DateTimeOffset dueAt) =>
        eventStore.ReadModels.Release(
            new ComplianceReadModelsRetentionDueSubject(stored.Id, dueAt, stored.Comment));
}
```
