```csharp
public record PartnerId(Guid Value) : EventSourceId<Guid>(Value);

// CHR0052: [Encrypted] cannot be applied to an EventSourceId<T> member.
public record PartnerIntegrationConfiguredWithId(
    [Encrypted] PartnerId PartnerId,
    string ApiKey);
```
