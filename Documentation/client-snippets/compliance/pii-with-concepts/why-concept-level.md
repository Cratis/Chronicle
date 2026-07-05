```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

// ❌ Property-level: requires repetition across every event
[EventType]
public record PiiConceptsComparisonEmployeeRegistered([PII] string Name, string Department);

[EventType]
public record PiiConceptsComparisonEmployeeNameChanged([PII] string NewName);  // must remember [PII] again

// ✅ Concept-level: declare once, apply everywhere automatically
[PII]
public record PiiConceptsComparisonPersonName(string Value) : ConceptAs<string>(Value)
{
    public static readonly PiiConceptsComparisonPersonName NotSet = new(string.Empty);
    public static implicit operator string(PiiConceptsComparisonPersonName name) => name.Value;
    public static implicit operator PiiConceptsComparisonPersonName(string value) => new(value);
}

[EventType]
public record PiiConceptsComparisonEmployeeRegisteredGood(PiiConceptsComparisonPersonName Name, string Department);  // Name is encrypted

[EventType]
public record PiiConceptsComparisonEmployeeNameChangedGood(PiiConceptsComparisonPersonName NewName);  // also encrypted, no extra annotation needed
```
