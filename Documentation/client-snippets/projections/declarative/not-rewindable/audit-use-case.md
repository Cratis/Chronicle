```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecNotRewindableUserLoginAttempt(string UserId, bool Succeeded);

[EventType]
public record DecNotRewindablePermissionChange(string UserId, string Permission);

public record DecNotRewindableSecurityAuditEntry(
    DateTimeOffset AuditedAt,
    long SequenceNumber);

public class DecNotRewindableSecurityAuditProjection : IProjectionFor<DecNotRewindableSecurityAuditEntry>
{
    public void Define(IProjectionBuilderFor<DecNotRewindableSecurityAuditEntry> builder) => builder
        .NotRewindable()
        .AutoMap()
        .FromEvery(_ => _
            .Set(m => m.AuditedAt).ToEventContextProperty(c => c.Occurred)
            .Set(m => m.SequenceNumber).ToEventContextProperty(c => c.SequenceNumber))
        .From<DecNotRewindableUserLoginAttempt>()
        .From<DecNotRewindablePermissionChange>();
}
```
