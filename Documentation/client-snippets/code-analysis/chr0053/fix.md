```csharp
// Personal data with a lawful basis for erasure:
public record Chr0053CustomerRegisteredPii([PII] string SomeValue);

// An operational secret with no data subject:
public record Chr0053CustomerRegisteredEncrypted([Encrypted] string SomeValue);
```
