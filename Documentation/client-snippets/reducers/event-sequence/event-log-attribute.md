```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

[EventType]
public record ReducersEventSequenceLogOrderPlaced(Guid OrderId);

public record ReducersEventSequenceLocalOrderSummary(int OrderCount, DateTimeOffset LastOrderAt);

[EventLog]
public class ReducersEventSequenceLocalOrderSummaryReducer : IReducerFor<ReducersEventSequenceLocalOrderSummary>
{
    public ReducersEventSequenceLocalOrderSummary Placed(ReducersEventSequenceLogOrderPlaced @event, ReducersEventSequenceLocalOrderSummary? current, EventContext context)
    {
        var count = current?.OrderCount ?? 0;
        return new ReducersEventSequenceLocalOrderSummary(count + 1, context.Occurred);
    }
}
```
