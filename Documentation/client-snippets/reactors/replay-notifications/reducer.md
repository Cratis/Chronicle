```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;

[EventType]
public record ReplayNotifiedItemAdded(string OrderId);

public record ReplayNotifiedOrderTotals(int Items);

public class ReplayNotifiedOrderTotalsReducer : IReducerFor<ReplayNotifiedOrderTotals>, ICanBeNotifiedWhenReplay
{
    public Task BeginReplay() => Task.CompletedTask;

    public Task EndReplay() => Task.CompletedTask;

    public ReplayNotifiedOrderTotals ItemAdded(ReplayNotifiedItemAdded @event, ReplayNotifiedOrderTotals? current) =>
        new((current?.Items ?? 0) + 1);
}
```
