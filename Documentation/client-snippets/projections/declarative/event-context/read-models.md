```csharp
public record DecEventContextUserActivity(
    string UserId,
    DateTimeOffset LastLogin,
    DateTimeOffset LastActivity);

public record DecEventContextAuditEntry(
    ulong EventId,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string ActionType,
    string UserId);
```
