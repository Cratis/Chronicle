```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecIndexUserRegistered(string Name, string Email, DateTimeOffset RegisteredAt);

public record DecIndexUserProfile(string Name, string Email, DateTimeOffset RegisteredAt);

public class DecIndexUserProfileProjection : IProjectionFor<DecIndexUserProfile>
{
    public void Define(IProjectionBuilderFor<DecIndexUserProfile> builder) => builder
        .From<DecIndexUserRegistered>();
}
```
