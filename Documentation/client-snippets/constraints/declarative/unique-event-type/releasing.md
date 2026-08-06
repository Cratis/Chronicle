```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueEventTypeShiftStarted(string Location);

[EventType]
public record ConstraintsUniqueEventTypeShiftEnded;

public class ConstraintsUniqueEventTypeOneOpenShift : IConstraint
{
    // At most one open shift per employee. Ending the shift releases the constraint,
    // so the next shift is allowed - without it the constraint could only say
    // "at most one, ever", and the employee's second shift would be refused forever.
    public void Define(IConstraintBuilder builder) =>
        builder
            .Unique<ConstraintsUniqueEventTypeShiftStarted>()
            .RemovedWith<ConstraintsUniqueEventTypeShiftEnded>();
}
```
