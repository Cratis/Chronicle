```csharp
public record DecNotRewindableAuditLogEntry(
    string UserId,
    string Action,
    string Details,
    DateTimeOffset OccurredAt,
    DateTimeOffset ProcessedAt,
    long SequenceNumber);
```
