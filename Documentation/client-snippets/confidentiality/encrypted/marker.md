```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class EncryptedAttribute(EncryptionScope scope = EncryptionScope.Subject, string details = "") : Attribute
{
    public EncryptionScope Scope { get; } = scope;
    public string Details { get; } = details;
}
```
