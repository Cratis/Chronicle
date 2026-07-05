```csharp title="Declarative FromEvery"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record UserCreatedDeclarativeEvery(string Name, string Email);

[EventType]
public record UserEmailChangedDeclarativeEvery(string Email);

public record UserProfileDeclarativeEvery(
    string Name,
    string Email,
    DateTimeOffset LastUpdated);

public class UserProfileDeclarativeEveryProjection : IProjectionFor<UserProfileDeclarativeEvery>
{
    public void Define(IProjectionBuilderFor<UserProfileDeclarativeEvery> builder) => builder
        .From<UserCreatedDeclarativeEvery>()
        .From<UserEmailChangedDeclarativeEvery>()
        .FromEvery(_ => _
            .Set(m => m.LastUpdated)
            .ToEventContextProperty(c => c.Occurred));
}
```
