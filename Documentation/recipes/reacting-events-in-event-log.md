# Reacting to events in the event log

One of the benefits of a system that oriented around events and event sourcing is that you
can easily quite independently add new functionality that can observe what has happened in
the system to react to state changes. All observers work independently and can be expanded
on whenever new requirements come to rise.

Chronicle will automatically play forward all existing events that any reactor are interested in.
The reactor marks their interest in event types and from that the system will know what to
forward to it.

Implementing a reactor based on creating a class with methods that handles the different
event types one is interested in. The methods are discovered automatically and follows a convention
that recognizes two signatures:

- `public Task <MethodName>(<EvenType> @event)`
- `public Task <MethodName>(<EvenType> @event, EventContext context)`

The latter signature includes the `EventContext` which gives you more details about the event,
such as the unique identifier it was applied for (`EventSourceId`). This type is found in `Cratis.Events`.

In addition to this, the class itself needs to be adorned with an attribute telling the system
it is an observer. The observers needs to be uniquely identified with a unique identifier.
This is first and required parameter of the attribute, in the form of a string representation
of a [Guid](https://docs.microsoft.com/en-us/dotnet/api/system.guid?view=net-6.0).

The following is a sample of an observer:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class DebitAccountObserver : IReactor
{
    public Task Opened(DebitAccountOpened @event, EventContext context)
    {
        // Perform any operation
        return Task.CompletedTask;
    }
}
```

If your observer fails for some reason and throws an exception. The error is captured and
the **partition** (The `EventSourceId`) is registered as failing and no further events for
that **partition** will be forwarded to the observer until the problem is resolved.

Chronicle will retry the **partition** on a scheduled timer. Once resolved it will carry on from
where it left off and will observe any other events that has occurred after the failure.
