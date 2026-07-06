```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

[EventType]
public record MbPassiveSnapshotCreated(string Data);

[Passive]
public record MbPassiveHistoricalSnapshot(
    [Key]
    Guid Id,

    [SetFrom<MbPassiveSnapshotCreated>(nameof(MbPassiveSnapshotCreated.Data))]
    string Data);
```
