```csharp
[Encrypted]
public record PartnerApiKeyScoped(string Value) : ConceptAs<string>(Value);       // one key per partner (EncryptionScope.Subject, the default)

[Encrypted(EncryptionScope.Namespace)]
public record PartnerWebhookSecret(string Value) : ConceptAs<string>(Value); // one key for every partner in the namespace

[Encrypted(EncryptionScope.Global)]
public record LicenseToken(string Value) : ConceptAs<string>(Value);        // one key for the whole installation
```
