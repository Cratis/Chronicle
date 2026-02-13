# Events

Events are immutable facts that describe what happened in your system. Chronicle identifies events by their event type rather than the .NET CLR type, which makes the CLR type a convenient vessel for expressing intent and structure.

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record EmployeeRegistered(string FirstName, string LastName);
```

## Event Sequences and the Event Log

Events live in event sequences, which are ordered, append-only streams. Each event receives a monotonically increasing sequence number that makes it possible to replay events, resume processing, and reason about how far consumers have progressed.

Chronicle includes a specialized event sequence called the event log. It is the default sequence used throughout the system and is exposed through the `IEventLog` API.

```csharp
using Cratis.Chronicle.Events;

public class EmployeesController(IEventLog eventLog)
{
    public Task RegisterEmployee(string firstName, string lastName) =>
        eventLog.Append(Guid.NewGuid(), new EmployeeRegistered(firstName, lastName));
}
```

## Working with Events

Use these topics to append, read, and manage events:

- [Appending](appending.md)
- [Appending with tags](appending-with-tags.md)
- [Appending many](appending-many.md)
- [Getting events](getting-events.md)
- [Getting state](getting-state.md)
- [Concurrency](concurrency.md)
- [Cross-cutting properties](cross-cutting-properties.md)

