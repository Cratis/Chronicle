```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

public class Chr0019UserReadModelFixed
{
    public string Name { get; set; } = string.Empty;
}

[EventType]
public record Chr0019UserRegisteredFixed(string Name);

public class Chr0019UserProjectionFixed : IProjectionFor<Chr0019UserReadModelFixed>
{
    public void Define(IProjectionBuilderFor<Chr0019UserReadModelFixed> builder) =>
        builder.From<Chr0019UserRegisteredFixed>(_ => _
            .Set(x => x.Name)
            .To(e => e.Name)); // Now valid - simple member access
}
```
