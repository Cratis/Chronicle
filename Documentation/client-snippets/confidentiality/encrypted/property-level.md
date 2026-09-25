```csharp
public record PartnerIntegrationConfigured(
    [Encrypted] string ApiKey,
    string PartnerName);

// When this event is written, ApiKey is encrypted. PartnerName is stored as plaintext.
```
