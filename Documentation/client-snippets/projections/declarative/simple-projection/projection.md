```csharp
using Cratis.Chronicle.Projections;

public class DecSimpleUserProjection : IProjectionFor<DecSimpleUser>
{
    public void Define(IProjectionBuilderFor<DecSimpleUser> builder) => builder
        .From<DecSimpleUserCreated>();
}
```
