```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record ReplayAwareOrderPlaced(string OrderId);

public class ReplayAwareOrderReactor : IReactor
{
    public void SendConfirmation(ReplayAwareOrderPlaced @event)
    {
        // Runs as the event happens.
    }

    [Replay]
    public void RebuildProjectionCache(ReplayAwareOrderPlaced @event)
    {
        // Runs instead of SendConfirmation while the observer is replaying.
    }
}
```
