```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

[PII]
public record ComplianceClientEmailAddress(string Value) : ConceptAs<string>(Value)
{
    public static readonly ComplianceClientEmailAddress NotSet = new(string.Empty);
    public static implicit operator string(ComplianceClientEmailAddress email) => email.Value;
    public static implicit operator ComplianceClientEmailAddress(string value) => new(value);
}

[EventType]
public record ComplianceClientCustomerRegistered(
    ComplianceClientPersonName Name,            // encrypted via concept type
    ComplianceClientEmailAddress Email,         // encrypted via concept type
    [PII] string PhoneNumber,                   // encrypted via property annotation
    string Country);                            // plaintext
```
