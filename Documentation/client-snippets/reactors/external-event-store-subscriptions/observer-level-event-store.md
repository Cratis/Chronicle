```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record ExternalEventStoreUserInvited(Guid UserId);

[EventStore("identity-service")]
[Reactor]
public class ExternalEventStoreUserInvitedReactor : IReactor
{
    public Task Invited(ExternalEventStoreUserInvited @event, EventContext context) => Task.CompletedTask;
}
```
