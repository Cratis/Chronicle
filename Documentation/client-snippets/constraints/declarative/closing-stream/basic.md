```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsDeclarativeClosingReportDelivered(string Recipient);

public class ConstraintsDeclarativeClosingReportDelivery : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.ClosesStreamOn<ConstraintsDeclarativeClosingReportDelivered>(
            closes => closes.PerEventSourceId().PerEventStreamType());
}
```
