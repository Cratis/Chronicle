```csharp
[Encrypted]
public record SecurityOverviewPartnerApiKey(string Value) : ConceptAs<string>(Value);

public record SecurityOverviewPartnerIntegrationConfigured(SecurityOverviewPartnerApiKey ApiKey);
```
