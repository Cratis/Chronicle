```csharp title="Composite audit key and read model"
public record AuditEntryKey(string UserId, DateTimeOffset Timestamp);

public record AuditEntryWithCompositeKey(
    AuditEntryKey Id,
    string Action,
    string Details);
```
