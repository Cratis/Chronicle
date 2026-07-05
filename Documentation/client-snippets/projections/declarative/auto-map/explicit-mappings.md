```csharp title="AutoMap with explicit mappings"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record AutoMapAccountOpened(string Name, string Email);

[EventType]
public record AutoMapAccountEmailChanged(string Email);

public record AutoMapAccount(
    string Name,
    string Email,
    string Status,
    DateTimeOffset CreatedAt);

public class AutoMapAccountProjection : IProjectionFor<AutoMapAccount>
{
    public void Define(IProjectionBuilderFor<AutoMapAccount> builder) => builder
        .From<AutoMapAccountOpened>(_ => _
            .Set(m => m.Status).ToValue("Active")
            .Set(m => m.CreatedAt).ToEventContextProperty(c => c.Occurred))
        .From<AutoMapAccountEmailChanged>();
}
```
