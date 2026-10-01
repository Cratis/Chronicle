```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

[EventType]
public record ReplayNotifiedOrderPlaced(string OrderId);

public interface IReplayNotificationGate
{
    void Pause();

    void Resume();
}

public class ReplayNotifiedOrderReactor(IReplayNotificationGate gate) : IReactor, ICanBeNotifiedWhenReplay
{
    public Task BeginReplay()
    {
        gate.Pause();
        return Task.CompletedTask;
    }

    public Task EndReplay()
    {
        gate.Resume();
        return Task.CompletedTask;
    }

    public Task OrderPlaced(ReplayNotifiedOrderPlaced @event)
    {
        // Handles the event both live and during the replay.
        return Task.CompletedTask;
    }
}
```
