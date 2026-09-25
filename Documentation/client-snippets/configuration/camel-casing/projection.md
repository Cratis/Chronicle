```csharp
using Cratis.Chronicle.Projections;

public class CamelCasingUserProjection : IProjectionFor<CamelCasingUserReadModel>
{
    public void Define(IProjectionBuilderFor<CamelCasingUserReadModel> builder) => builder
        .From<CamelCasingUserRegistered>();
}
```
