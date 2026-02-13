# OnceOnly

The `OnceOnly` attribute is used to mark reactor methods that should only execute once and never called again,
for instance during a replay.

There are scenarios where you need side effects to occur only on the initial event processing,
such as sending notifications, triggering external integrations, or performing non-idempotent operations.

```csharp
using Cratis.Chronicle.Reactors;

public class MyReactor : IReactor
{
    [OnceOnly]
    public void SendNotification(MyEvent @event)
    {
        // This code will only execute once when the event is first processed,
        // and will be skipped during replay.
    }
}
```

The `OnceOnly` attribute ensures that the marked method is executed exactly once,
providing a clean way to separate one-time operations from normal reactor behavior.
