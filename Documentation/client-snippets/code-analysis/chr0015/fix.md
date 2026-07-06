```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record Chr0015UserRegisteredFixed;

public class Chr0015UserReadModelFixed;

public class Chr0015UserProjectionFixed : IProjectionFor<Chr0015UserReadModelFixed>
{
    // Now valid - no constructor dependencies
    public void Define(IProjectionBuilderFor<Chr0015UserReadModelFixed> builder) =>
        builder.From<Chr0015UserRegisteredFixed>();
}
```
