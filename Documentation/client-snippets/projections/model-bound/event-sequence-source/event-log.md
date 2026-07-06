```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Keys;

[EventType]
public record MbEventSeqLocalEvent(string Data);

[EventLog]
public record MbEventSeqLocalSnapshot(
    [Key]
    Guid Id,

    [SetFrom<MbEventSeqLocalEvent>(nameof(MbEventSeqLocalEvent.Data))]
    string Data);
```
