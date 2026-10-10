```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ClosingEventsVersionSuperseded(string Version);

[EventType]
public record ClosingEventsVersionReinstated(string Version);

public class ClosingEventsVersionClosing : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.ClosesStreamOn<ClosingEventsVersionSuperseded>(
            closes => closes
                .PerEventSourceId()
                .EventStreamIdFrom(@event => @event.Version)
                .ReopenedBy<ClosingEventsVersionReinstated>(),
            name: "close-superseded-version");
}
```
