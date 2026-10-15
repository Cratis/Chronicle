```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsDeclarativeClosingReportDeliveredToClient(string Version);

[EventType]
public record ConstraintsDeclarativeClosingReportSuperseded(string Version);

public class ConstraintsDeclarativeClosingVersions : IConstraint
{
    public void Define(IConstraintBuilder builder)
    {
        // Two declarations that share a name merge into one constraint. They must use the same dimensions.
        builder.ClosesStreamOn<ConstraintsDeclarativeClosingReportDeliveredToClient>(
            closes => closes.PerEventSourceId().EventStreamIdFrom(@event => @event.Version),
            name: "close-report-version");
        builder.ClosesStreamOn<ConstraintsDeclarativeClosingReportSuperseded>(
            closes => closes.PerEventSourceId().EventStreamIdFrom(@event => @event.Version),
            name: "close-report-version");
    }
}
```
