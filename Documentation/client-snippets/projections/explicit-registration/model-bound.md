```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record SupplierOnboarded(string Name);

[FromEvent<SupplierOnboarded>]
public record Supplier(string Name);

public class SupplierRegistration
{
    public Task Register(IEventStore eventStore) => eventStore.ReadModels.Register<Supplier>();
}
```
