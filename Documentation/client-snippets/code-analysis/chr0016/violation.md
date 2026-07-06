```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record Chr0016UserRegistered;

public class Chr0016UserReadModel;

public class Chr0016UserProjection : IProjectionFor<Chr0016UserReadModel>
{
    public bool SomeCondition { get; init; }

    // CHR0016: Define must not contain conditional logic
    public void Define(IProjectionBuilderFor<Chr0016UserReadModel> builder)
    {
        if (SomeCondition)
        {
            builder.From<Chr0016UserRegistered>();
        }
    }
}
```
