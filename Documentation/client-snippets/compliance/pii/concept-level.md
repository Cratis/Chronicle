```csharp
using Cratis.Chronicle.Compliance.GDPR;

[PII]
public record PiiAttrConceptPersonName(string Value) : ConceptAs<string>(Value)
{
    public static readonly PiiAttrConceptPersonName NotSet = new(string.Empty);
    public static implicit operator string(PiiAttrConceptPersonName name) => name.Value;
    public static implicit operator PiiAttrConceptPersonName(string value) => new(value);
}
```
