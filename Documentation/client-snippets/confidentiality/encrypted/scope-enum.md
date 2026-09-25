```csharp
public enum EncryptionScope
{
    Subject,    // per compliance identity - the default
    Namespace,  // one key shared by every value marked this way in the event store namespace
    Global      // one key shared by every value marked this way across the whole installation
}
```
