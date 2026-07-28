```csharp
using Cratis.Chronicle.Compliance.GDPR;

[PII]
public record PiiAttrDateOfBirth(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator PiiAttrDateOfBirth(string value) => new(value);
}

// The concept sits one level down, inside a value object.
public record PiiAttrVerifiedDateOfBirth(PiiAttrDateOfBirth DateOfBirth, string VerifiedBy);

// Chronicle still finds it: dateOfBirth.dateOfBirth is encrypted, verifiedBy is not.
public record PiiAttrExpressVerification(string Name, PiiAttrVerifiedDateOfBirth DateOfBirth);
```
