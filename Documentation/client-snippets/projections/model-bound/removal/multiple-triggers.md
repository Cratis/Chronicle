```csharp
[EventType]
public record MbRemovalMultipleAccountOpened(string Name);

[EventType]
public record MbRemovalMultipleAccountClosed;

[EventType]
public record MbRemovalMultipleAccountMerged(Guid SourceAccountId);

[RemovedWith<MbRemovalMultipleAccountClosed>]
[RemovedWith<MbRemovalMultipleAccountMerged>(key: nameof(MbRemovalMultipleAccountMerged.SourceAccountId))]
public record MbRemovalMultipleAccount(
    [Key]
    Guid Id,

    [SetFrom<MbRemovalMultipleAccountOpened>(nameof(MbRemovalMultipleAccountOpened.Name))]
    string Name);
```
