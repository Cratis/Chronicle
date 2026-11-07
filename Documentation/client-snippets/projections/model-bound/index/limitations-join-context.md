```csharp title="Map the joined event's context"
using System;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record JoinAuditOrderPlaced(Guid ProductId);

[EventType]
public record JoinAuditProductRenamed(string Name);

public record JoinAuditOrder(Guid ProductId, string ProductName, DateTimeOffset ProductRenamedAt);

public class JoinAuditOrderProjection : IProjectionFor<JoinAuditOrder>
{
    public void Define(IProjectionBuilderFor<JoinAuditOrder> builder) => builder
        .From<JoinAuditOrderPlaced>()
        .Join<JoinAuditProductRenamed>(join => join
            .On(m => m.ProductId)
            .Set(m => m.ProductName).To(e => e.Name)
            .Set(m => m.ProductRenamedAt).ToEventContextProperty(c => c.Occurred));
}
```
