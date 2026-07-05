```csharp title="Disable AutoMap"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record AutoMapDisabledAccountRegistered(string AccountName, string ContactEmail);

public record AutoMapDisabledAccount(
    string Name,
    string Email,
    DateTimeOffset CreatedAt);

public class AutoMapDisabledAccountProjection : IProjectionFor<AutoMapDisabledAccount>
{
    public void Define(IProjectionBuilderFor<AutoMapDisabledAccount> builder) => builder
        .NoAutoMap()
        .From<AutoMapDisabledAccountRegistered>(_ => _
            .Set(m => m.Name).To(e => e.AccountName)
            .Set(m => m.Email).To(e => e.ContactEmail)
            .Set(m => m.CreatedAt).ToEventContextProperty(c => c.Occurred));
}
```
