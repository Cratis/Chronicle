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

// The command that appends the event is one of the two placements that work - here the value
// tags the appended events and, with concurrency, joins the server-side concurrency scope.
[EventStreamType("onboarding", concurrency: true)]
[Command]
public record Chr0045OpenAccount(string Name)
{
    public Chr0045AccountOpened Handle() => new(Name);
}

// The observer is the other - here the value narrows which appended events are dispatched.
[EventStreamType("onboarding")]
public class Chr0045AccountNotifier : IReactor
{
    public Task AccountOpened(Chr0045AccountOpened @event) => Task.CompletedTask;
}
```
