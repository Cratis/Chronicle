```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record WelcomeEmailRequested(string Email);

[StartAtTail]
public class SendWelcomeEmails : IReactor
{
    public Task Requested(WelcomeEmailRequested @event)
    {
        // Runs for events appended after this reactor was first registered.
        // It does not run for the history that was already in the event log.
        return Task.CompletedTask;
    }
}
```
