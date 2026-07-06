```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbRemovalAccountOpened(string Name, decimal Balance);

[EventType]
public record MbRemovalAccountClosed;

[RemovedWith<MbRemovalAccountClosed>]
public record MbRemovalAccount(
    [Key]
    Guid Id,

    [SetFrom<MbRemovalAccountOpened>(nameof(MbRemovalAccountOpened.Name))]
    string Name,

    [SetFrom<MbRemovalAccountOpened>(nameof(MbRemovalAccountOpened.Balance))]
    decimal Balance);
```
