```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record OnceOnlyOrderPlaced(string OrderId);

public class OnceOnlyOrderReactor : IReactor
{
    [OnceOnly]
    public void SendNotification(OnceOnlyOrderPlaced @event)
    {
        // This code will only execute once when the event is first processed,
        // and will be skipped during replay.
    }
}
```
