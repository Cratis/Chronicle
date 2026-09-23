```csharp
public record Chr0052PartnerIdFixed(Guid Value) : EventSourceId<Guid>(Value);

public record Chr0052PartnerIntegrationConfiguredFixed(
    Chr0052PartnerIdFixed PartnerId,
    [Encrypted] string ApiKey);
```
