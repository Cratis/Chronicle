```csharp
[EventType]
public record MbRemovalWithKeyAccountOpened(string Name);

[EventType]
public record MbRemovalWithKeyAccountClosed(Guid AccountId);

[RemovedWith<MbRemovalWithKeyAccountClosed>(key: nameof(MbRemovalWithKeyAccountClosed.AccountId))]
public record MbRemovalWithKeyAccount(
    [Key]
    Guid Id,

    [SetFrom<MbRemovalWithKeyAccountOpened>(nameof(MbRemovalWithKeyAccountOpened.Name))]
    string Name);
```
