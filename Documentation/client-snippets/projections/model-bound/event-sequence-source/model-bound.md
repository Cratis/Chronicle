```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbEventSeqOrderPlaced(decimal Amount);

[EventSequence("custom-sequence")]
public record MbEventSeqOrderSummary(
    [Key]
    Guid Id,

    [SetFrom<MbEventSeqOrderPlaced>(nameof(MbEventSeqOrderPlaced.Amount))]
    decimal TotalAmount);
```
