```csharp
// Throws PIIAndEncryptedCombinedNotSupported at schema-generation time,
// and is flagged at compile time by CHR0053.
public record EncryptedCustomerRegistered(
    [PII] [Encrypted] string SomeValue);
```
