```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

// Warning CHR0045: 'EventStreamTypeAttribute' on event type 'Chr0045AccountOpened' has no
// effect - an append resolves its stream metadata from the append itself, never from the
// event's type. Every append of this event still lands under the default stream type, and a
// reactor filtering on "onboarding" observes none of them.
[EventStreamType("onboarding")]
[EventType]
public record Chr0045AccountOpened(string Name);

// The command that appends the event is a placement that works - here the value tags the
// appended events and, with concurrency, joins the server-side concurrency scope.
[EventStreamType("onboarding", concurrency: true)]
[Command]
public record Chr0045OpenAccount(string Name)
{
    public Chr0045AccountOpened Handle() => new(Name);
}

// The observer is another - here the value narrows which appended events are dispatched. An
// aggregate root is the third: [EventStreamType] there becomes the stream type of every event
// the aggregate appends, and the aggregate's type name is used when it is absent.
[EventStreamType("onboarding")]
public class Chr0045AccountNotifier : IReactor
{
    public Task AccountOpened(Chr0045AccountOpened @event) => Task.CompletedTask;
}
```
