# Events

Events are recognized by their type, not the .NET CLR Type, but its unique identifier.
The CLR type is just the vessel it shows up in when one is committing or reacting
to an event. In its nature, events in an event sourced system are immutable.
With C# 9 we got the construct `record` which is perfect for this.

Below is an example of what an event could be:

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record EmployeeRegistered(string FirstName, string LastName);
```

## EventLog

To save an event to the event log, all you need is to take a dependency to `IEventLog`
and call the appropriate `Append` method.

```csharp
using Cratis.Chronicle.Events;

[Route("/api/employees")]
public class EmployeesController(IEventLog eventLog) : ControllerBase
{
    [HttpPost("registration")]
    public Task Register() =>
        eventLog.Append(Guid.NewGuid(), new EmployeeRegistered(..., ....));
}
```

## Cross cutting properties

You can add properties to all events and types being committed, without having to rely on them
being explicitly defined on the type and then having to be set throughout.
Typical use-case would be some metadata that you want to capture that is beyond the domain
in which the event exists in. And also those properties that you want to make sure aren't tampered
with and should be provided by the backend.

All you need to do is create a class that implements the `ICanProvideAdditionalEventInformation`
interface:

```csharp
using Cratis.Chronicle.Events;

public class MyAdditionalEventInformationProvider : ICanProvideAdditionalEventInformation
{
    public Task ProvideFor(JsonObject @event)
    {
        @event.Add("something", Guid.NewGuid());
        @event.Add("someTime", DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}
```

This will automatically be picked up and used by the mechanisms for appending events.
