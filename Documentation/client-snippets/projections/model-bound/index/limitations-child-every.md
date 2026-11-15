```csharp title="Child every-mapping with a join-only event"
using System;
using System.Collections.Generic;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record ChildAuditOrderPlaced(string CustomerName);

[EventType]
public record ChildAuditItemAddedToOrder(Guid ProductId, int Quantity);

[EventType]
public record ChildAuditProductRenamed(string Name);

public record ChildAuditLine(Guid ProductId, int Quantity, string ProductName, DateTimeOffset LastUpdated);
public record ChildAuditOrder(string CustomerName, IEnumerable<ChildAuditLine> Lines);

public class ChildAuditOrderProjection : IProjectionFor<ChildAuditOrder>
{
    public void Define(IProjectionBuilderFor<ChildAuditOrder> builder) => builder
        .From<ChildAuditOrderPlaced>()
        .Children(m => m.Lines, children => children
            .IdentifiedBy(line => line.ProductId)
            .From<ChildAuditItemAddedToOrder>(mapping => mapping
                .UsingKey(e => e.ProductId)
                .UsingParentKeyFromContext(c => c.EventSourceId))
            .FromEvery(every => every
                .Set(line => line.LastUpdated).ToEventContextProperty(c => c.Occurred))
            .Join<ChildAuditProductRenamed>(join => join
                .Set(line => line.ProductName).To(e => e.Name)));
}
```
