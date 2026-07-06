```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record MbIndexAutoMapAccountOpened(string Name, decimal Balance);

public class MbIndexAutoMapAccountProjection : IProjectionFor<MbIndexAutoMapAccountInfo>
{
    public void Define(IProjectionBuilderFor<MbIndexAutoMapAccountInfo> builder) => builder
        .AutoMap()
        .From<MbIndexAutoMapAccountOpened>();
}

public class MbIndexAutoMapAccountInfo
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
```
