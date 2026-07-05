```csharp
using Cratis.Chronicle.Compliance.GDPR;

[PII("Collected under GDPR Art. 6(1)(b) — necessary for contract performance. Retention: contract duration + 7 years.")]
public record PiiConceptsLegalName(string Value) : ConceptAs<string>(Value)
{
    public static readonly PiiConceptsLegalName NotSet = new(string.Empty);

    public static implicit operator string(PiiConceptsLegalName name) => name.Value;
    public static implicit operator PiiConceptsLegalName(string value) => new(value);
}
```
