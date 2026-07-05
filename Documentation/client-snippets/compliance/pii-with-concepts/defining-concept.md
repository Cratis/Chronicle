```csharp
using Cratis.Chronicle.Compliance.GDPR;

[PII]
public record PiiConceptsPersonName(string Value) : ConceptAs<string>(Value)
{
    public static readonly PiiConceptsPersonName NotSet = new(string.Empty);

    public static implicit operator string(PiiConceptsPersonName name) => name.Value;
    public static implicit operator PiiConceptsPersonName(string value) => new(value);
}
```
