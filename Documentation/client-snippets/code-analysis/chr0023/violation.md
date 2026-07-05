```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record Chr0023Ledger(
    [Key] Guid Id,

    // Warning CHR0023: Chr0023EntryRecorded has two Guid-typed properties (LedgerId and
    // OffsetLedgerId), so Chronicle cannot infer which one links an entry to its parent ledger.
    [ChildrenFrom<Chr0023EntryRecorded>(key: nameof(Chr0023EntryRecorded.EntryId))]
    IEnumerable<Chr0023Entry> Entries);

[EventType]
public record Chr0023EntryRecorded(Guid EntryId, Guid LedgerId, Guid OffsetLedgerId, decimal Amount);

public record Chr0023Entry([Key] Guid EntryId, decimal Amount);
```
