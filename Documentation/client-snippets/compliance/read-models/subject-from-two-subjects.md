```csharp
using Cratis.Chronicle.Compliance.GDPR;

[PII]
public record ComplianceReadModelsPersonFullName(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator ComplianceReadModelsPersonFullName(string value) => new(value);
}

// A duplicate-review row about two people, keyed by a contact-point hash that is nobody. Each name
// is released under the person it belongs to.
public record ComplianceReadModelsDuplicatePair(
    string ContactPointHash,
    string FirstPersonId,
    string SecondPersonId,
    [SubjectFrom(nameof(FirstPersonId))] ComplianceReadModelsPersonFullName FirstName,
    [SubjectFrom(nameof(SecondPersonId))] ComplianceReadModelsPersonFullName SecondName);
```
