```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

[EventType]
public record Chr0017UserRegistered;

public class Chr0017UniqueEmailConstraint : IConstraint
{
    readonly IEventLog _eventLog;

    // CHR0017: Constraints must not take dependencies through the constructor
    public Chr0017UniqueEmailConstraint(IEventLog eventLog)
    {
        _eventLog = eventLog;
    }

    public void Define(IConstraintBuilder builder) =>
        builder.Unique<Chr0017UserRegistered>();
}
```
