```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsDeclarativeClosingBooksClosed(string Period, decimal ClosingBalance);

public class ConstraintsDeclarativeClosingBooks : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.ClosesStreamOn<ConstraintsDeclarativeClosingBooksClosed>(
            closes => closes.PerEventSourceId().EventStreamIdFrom(@event => @event.Period));
}
```
