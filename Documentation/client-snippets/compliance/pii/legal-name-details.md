```csharp
using Cratis.Chronicle.Compliance.GDPR;

[PII("Full legal name — required for contract identification")]
public record PiiAttrLegalName(string Value) : ConceptAs<string>(Value);
```
