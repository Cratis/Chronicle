# Not Rewindable

Mark a projection as not rewindable (forward-only) using `[NotRewindable]`:

```csharp
[NotRewindable]
public record AuditLog(
    [Key]
    Guid Id,

    [SetFrom<AuditEvent>]
    string Message,

    [SetFrom<AuditEvent>]
    DateTimeOffset Timestamp);
```

## When to use

- Audit logs or append-only data
- Projections that should never be replayed from scratch
- Time-sensitive data where replay would be incorrect

## Best practices

- Use for append-only scenarios where historical replay would be problematic
- Consider carefully as this removes the ability to rebuild the projection from scratch
- Ensure you have alternative recovery strategies if the projection becomes corrupted
