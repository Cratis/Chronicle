```csharp
using Cratis.Chronicle.Projections;

public class CamelCasingUserProjection : IProjectionFor<CamelCasingUserReadModel>
{
    public void Define(IProjectionBuilderFor<CamelCasingUserReadModel> builder) => builder
        .From<CamelCasingUserRegistered>(_ => _
            .Set(m => m.FirstName).To(e => e.FirstName)
            .Set(m => m.LastName).To(e => e.LastName)
            .Set(m => m.EmailAddress).To(e => e.EmailAddress)
            .Set(m => m.RegistrationDate).To(e => e.RegistrationDate));
}
```
