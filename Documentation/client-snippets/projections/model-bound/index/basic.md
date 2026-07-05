```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbIndexAccountOpened(string Name, decimal InitialBalance);

[FromEvent<MbIndexAccountOpened>]
public record MbIndexAccountInfo(
    [Key]
    Guid Id,

    string Name,

    [SetFrom<MbIndexAccountOpened>(nameof(MbIndexAccountOpened.InitialBalance))]
    decimal Balance);
```
