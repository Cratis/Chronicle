```csharp
public record Chr0023LedgerFixed(
    [Key] Guid Id,

    // parentKey names the event property that links the child back to its parent, so the
    // relationship is explicit and no longer depends on declaration order.
    [ChildrenFrom<Chr0023EntryRecordedFixed>(
        key: nameof(Chr0023EntryRecordedFixed.EntryId),
        parentKey: nameof(Chr0023EntryRecordedFixed.LedgerId))]
    IEnumerable<Chr0023EntryFixed> Entries);

[EventType]
public record Chr0023EntryRecordedFixed(Guid EntryId, Guid LedgerId, Guid OffsetLedgerId, decimal Amount);

public record Chr0023EntryFixed([Key] Guid EntryId, decimal Amount);
```
