```csharp
using Cratis.Chronicle.Compliance.GDPR;

[PII("National ID number — sensitive personal identifier")]
public record PiiConceptsNationalIdNumber(string Value) : ConceptAs<string>(Value)
{
    public static readonly PiiConceptsNationalIdNumber NotSet = new(string.Empty);

    public static implicit operator string(PiiConceptsNationalIdNumber id) => id.Value;
    public static implicit operator PiiConceptsNationalIdNumber(string value) => new(value);
}
```
