```csharp
[EventType]
public record MbRemovalJoinClassEmployeeHired(string Name);

[EventType]
public record MbRemovalJoinClassCompanyRegistered(string Name);

[EventType]
public record MbRemovalJoinClassCompanyDissolved;

[RemovedWithJoin<MbRemovalJoinClassCompanyDissolved>]
public record MbRemovalJoinClassEmployee(
    [Key]
    Guid Id,

    [SetFrom<MbRemovalJoinClassEmployeeHired>(nameof(MbRemovalJoinClassEmployeeHired.Name))]
    string Name,

    [Join<MbRemovalJoinClassCompanyRegistered>(eventPropertyName: nameof(MbRemovalJoinClassCompanyRegistered.Name))]
    string CompanyName);
```
