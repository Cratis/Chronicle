```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

public class Chr0019UserReadModel
{
    public string Name { get; set; } = string.Empty;
}

[EventType]
public record Chr0019UserRegistered(string Name);

public class Chr0019UserProjection : IProjectionFor<Chr0019UserReadModel>
{
    public void Define(IProjectionBuilderFor<Chr0019UserReadModel> builder) =>
        builder.From<Chr0019UserRegistered>(_ => _
            .Set(x => x.Name)
            .To(e => e.Name.ToUpper())); // CHR0019: method call - never executed
}
```
