```csharp title="Clear one member of a nested object, or the whole object"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbClearingContractSigned(string Title, string NoticeGiven);

[EventType]
public record MbClearingNoticeWithdrawn;

[EventType]
public record MbClearingContractEnded;

[FromEvent<MbClearingContractSigned>]
public record MbClearingContract(
    string Title,

    // Clears this member of the nested object; the object itself stays.
    [ClearWith<MbClearingNoticeWithdrawn>]
    string? NoticeGiven);

public record MbClearingEmployee(
    [Key] Guid Id,

    // Clears the whole nested object back to null.
    [Nested]
    [ClearWith<MbClearingContractEnded>]
    MbClearingContract? Contract);
```
