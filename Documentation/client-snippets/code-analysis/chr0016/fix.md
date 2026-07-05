```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record Chr0016UserRegisteredFixed;

public class Chr0016UserReadModelFixed;

public class Chr0016UserProjectionFixed : IProjectionFor<Chr0016UserReadModelFixed>
{
    // Now valid - unconditional
    public void Define(IProjectionBuilderFor<Chr0016UserReadModelFixed> builder) =>
        builder.From<Chr0016UserRegisteredFixed>();
}
```
