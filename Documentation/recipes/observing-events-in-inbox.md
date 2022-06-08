# Observing events in the inbox

First thing one needs to do is for the microservice that owns the definition of the
public event should publish a package that your microservice can consume that holds
the definition of the event so that you can consume it.

Observing and reacting to events from the inbox is then done in the same
way as for [private events in the event log](./observing-events-in-event-log.md).
The only difference is that you have to tell the system to observe the **inbox**
rather than the event log. You specify this by adding `inbox: true` to the
`[Observer]` attribute.

Based on the public event as defined [here](./creating-a-public-event.md) we can
create an observer for this:

```csharp
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Events.Observation;

[Observer("292a21dc-71de-4042-a313-4bcd45f6e0cb", inbox: true)]
public class AccountBalanceObserver
{
    public Task Balance(AccountBalance @event, EventContext context)
    {
        Console.WriteLine($"Balance for {context.EventSourceId} : {@event.Balance}");
        return Task.CompletedTask;
    }
}
```
