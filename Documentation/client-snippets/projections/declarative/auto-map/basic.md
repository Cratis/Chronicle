```csharp title="AutoMap by convention"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record AutoMapUserCreated(string Name, string Email);

[EventType]
public record AutoMapUserRenamed(string Name);

public record AutoMapUser(string Name, string Email);

public class AutoMapUserProjection : IProjectionFor<AutoMapUser>
{
    public void Define(IProjectionBuilderFor<AutoMapUser> builder) => builder
        .From<AutoMapUserCreated>()
        .From<AutoMapUserRenamed>();
}
```
