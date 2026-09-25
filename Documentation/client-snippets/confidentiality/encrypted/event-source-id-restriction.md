```csharp
public record PartnerId(Guid Value) : EventSourceId<Guid>(Value);

// Throws EncryptedNotSupportedOnEventSourceId
public record PartnerIntegrationConfiguredWithId(
    [Encrypted] PartnerId PartnerId,
    string ApiKey);
```
