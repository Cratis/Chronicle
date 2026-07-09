```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

public record Chr0029Customer(string Name);

[EventType]
public record Chr0029CustomerRegistered(string Name);

public class Chr0029CustomerProjection : IProjectionFor<Chr0029Customer>
{
    public void Define(IProjectionBuilderFor<Chr0029Customer> builder) =>
        builder.From<Chr0029CustomerRegistered>(customer => customer
            // Warning CHR0029: '.Set(x => x.Name).To(e => e.Name)' is redundant — AutoMap already maps
            // identically named properties. Remove the mapping.
            .Set(x => x.Name).To(e => e.Name));
}
```
