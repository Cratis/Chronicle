```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Concepts;

[PII(SpecialCategory = SpecialCategory.Health)]
public record MedicalCondition(string Value) : ConceptAs<string>(Value);
```
