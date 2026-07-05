```csharp title="Declarative FromAll"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record UserCreatedDeclarativeAll(string Name, string Email);

[EventType]
public record UserEmailChangedDeclarativeAll(string Email);

public record UserProfileDeclarativeAll(
    string Name,
    string Email,
    DateTimeOffset LastUpdated);

public class UserProfileDeclarativeAllProjection : IProjectionFor<UserProfileDeclarativeAll>
{
    public void Define(IProjectionBuilderFor<UserProfileDeclarativeAll> builder) => builder
        .From<UserCreatedDeclarativeAll>()
        .From<UserEmailChangedDeclarativeAll>()
        .FromAll(_ => _
            .Set(m => m.LastUpdated)
            .ToEventContextProperty(c => c.Occurred));
}
```
