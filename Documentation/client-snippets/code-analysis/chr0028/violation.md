```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

public record Chr0028Product(string Name);

[EventType]
public record Chr0028ProductRegistered(string Name);

public class Chr0028ProductProjection : IProjectionFor<Chr0028Product>
{
    public void Define(IProjectionBuilderFor<Chr0028Product> builder) =>
        builder
            .From<Chr0028ProductRegistered>()
            // Warning CHR0028: '.AutoMap()' is redundant — AutoMap is enabled by default. Remove the call.
            .AutoMap();
}
```
