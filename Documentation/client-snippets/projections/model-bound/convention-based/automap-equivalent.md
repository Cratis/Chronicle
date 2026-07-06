```csharp title="Model-bound and declarative AutoMap"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record ConventionEquivalentUserRegistered(string Name, string Email);

[FromEvent<ConventionEquivalentUserRegistered>]
public record ConventionEquivalentUser(
    [Key] Guid Id,
    string Name,
    string Email);

public class ConventionEquivalentProjection : IProjectionFor<ConventionEquivalentUser>
{
    public void Define(IProjectionBuilderFor<ConventionEquivalentUser> builder) =>
        builder.From<ConventionEquivalentUserRegistered>();
}
```
