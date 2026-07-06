```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;

[EventType]
public record Chr0015UserRegistered;

public class Chr0015UserReadModel;

public class Chr0015UserProjection : IProjectionFor<Chr0015UserReadModel>
{
    readonly IEventLog _eventLog;

    // CHR0015: Projections must not take dependencies through the constructor
    public Chr0015UserProjection(IEventLog eventLog)
    {
        _eventLog = eventLog;
    }

    public void Define(IProjectionBuilderFor<Chr0015UserReadModel> builder) =>
        builder.From<Chr0015UserRegistered>();
}
```
