```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;

[EventType]
public record MbEventSeqFluentOrderPlaced(decimal Amount);

public class MbEventSeqFluentOrderProjection : IProjectionFor<MbEventSeqFluentOrderSummary>
{
    public void Define(IProjectionBuilderFor<MbEventSeqFluentOrderSummary> builder) => builder
        .FromEventSequence(new EventSequenceId("custom-sequence"))
        .From<MbEventSeqFluentOrderPlaced>(_ => _
            .Set(m => m.TotalAmount).To(e => e.Amount));
}

public class MbEventSeqFluentOrderSummary
{
    public decimal TotalAmount { get; set; }
}
```
