```csharp
using Cratis.Chronicle.Compliance.GDPR;

[PII("Collected under GDPR Art. 6(1)(b) — necessary for contract performance")]
public record PiiAttrPersonName(string Value) : ConceptAs<string>(Value);
```
