```csharp
[Encrypted]
public record PartnerApiKey(string Value) : ConceptAs<string>(Value);

public record PartnerIntegrationConfiguredWithKey(PartnerApiKey ApiKey);
```
