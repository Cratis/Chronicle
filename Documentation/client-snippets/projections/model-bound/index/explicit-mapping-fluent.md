```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record MbIndexExplicitAccountOpened(string Name, decimal InitialBalance);

public class MbIndexExplicitAccountProjection : IProjectionFor<MbIndexExplicitAccountInfo>
{
    public void Define(IProjectionBuilderFor<MbIndexExplicitAccountInfo> builder) => builder
        .From<MbIndexExplicitAccountOpened>(_ => _
            .Set(m => m.Name).To(e => e.Name)
            .Set(m => m.Balance).To(e => e.InitialBalance));
}

public class MbIndexExplicitAccountInfo
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
```
