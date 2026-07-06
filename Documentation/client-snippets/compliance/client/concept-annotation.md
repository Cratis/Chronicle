```csharp
using Cratis.Chronicle.Compliance.GDPR;

[PII]
public record ComplianceClientPersonName(string Value) : ConceptAs<string>(Value)
{
    public static readonly ComplianceClientPersonName NotSet = new(string.Empty);
    public static implicit operator string(ComplianceClientPersonName name) => name.Value;
    public static implicit operator ComplianceClientPersonName(string value) => new(value);
}
```
