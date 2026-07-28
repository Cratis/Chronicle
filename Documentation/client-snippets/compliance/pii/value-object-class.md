```csharp
using Cratis.Chronicle.Compliance.GDPR;

// Every value this type holds is personal, so mark the type once.
[PII]
public record PiiAttrDiagnosis(string Condition, string DiagnosedBy);

// Both Condition and DiagnosedBy are encrypted wherever a PiiAttrDiagnosis appears.
public record PiiAttrPatientRecord(string Name, PiiAttrDiagnosis Diagnosis);
```
