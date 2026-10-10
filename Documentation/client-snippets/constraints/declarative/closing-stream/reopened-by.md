```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsDeclarativeClosingMonthClosed(string Month);

[EventType]
public record ConstraintsDeclarativeClosingMonthReopened(string Month, string Reason);

public class ConstraintsDeclarativeClosingMonth : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.ClosesStreamOn<ConstraintsDeclarativeClosingMonthClosed>(
            closes => closes
                .PerEventSourceId()
                .EventStreamIdFrom(@event => @event.Month)
                .ReopenedBy<ConstraintsDeclarativeClosingMonthReopened>(),
            name: "close-month");
}
```
