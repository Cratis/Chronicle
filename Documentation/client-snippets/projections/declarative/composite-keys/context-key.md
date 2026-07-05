```csharp title="Composite key from event content and context"
using Cratis.Chronicle.Projections;

public class AuditEntryProjectionWithCompositeKey : IProjectionFor<AuditEntryWithCompositeKey>
{
    public void Define(IProjectionBuilderFor<AuditEntryWithCompositeKey> builder) => builder
        .From<CompositeUserAction>(action => action
            .UsingCompositeKey<AuditEntryKey>(key => key
                .Set(k => k.UserId).To(e => e.UserId)
                .Set(k => k.Timestamp).ToEventContextProperty(c => c.Occurred)));
}
```
