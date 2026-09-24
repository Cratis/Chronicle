```csharp
public record Chr0052PartnerId(Guid Value) : EventSourceId<Guid>(Value);

public record Chr0052PartnerIntegrationConfigured(
    [Encrypted] Chr0052PartnerId PartnerId,
    string ApiKey);
```
