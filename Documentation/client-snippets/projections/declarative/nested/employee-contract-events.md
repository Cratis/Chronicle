```csharp title="Employee contract events"
using Cratis.Chronicle.Events;

[EventType]
public record EmployeeHiredForNestedContractEvents(string Name, string Department);

[EventType]
public record ContractStartedForNestedContractEvents(Guid ContractId, DateOnly StartDate, DateOnly EndDate, string Type);

[EventType]
public record ContractExtendedForNestedContractEvents(DateOnly NewEndDate);

[EventType]
public record ContractEndedForNestedContractEvents;
```
