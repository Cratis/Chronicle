```csharp title="Employee contract projection"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record EmployeeHiredWithNestedContract(string Name, string Department);

[EventType]
public record ContractStartedWithNestedContract(Guid ContractId, DateOnly StartDate, DateOnly EndDate, string Type);

[EventType]
public record ContractExtendedWithNestedContract(DateOnly NewEndDate);

[EventType]
public record ContractEndedWithNestedContract;

public record EmployeeWithNestedContract(
    string Name,
    string Department,
    ContractForNestedEmployee? ActiveContract);

public record ContractForNestedEmployee(
    Guid ContractId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Type);

public class EmployeeProjectionWithNestedContract : IProjectionFor<EmployeeWithNestedContract>
{
    public void Define(IProjectionBuilderFor<EmployeeWithNestedContract> builder) => builder
        .From<EmployeeHiredWithNestedContract>()
        .Nested(m => m.ActiveContract, contract => contract
            .From<ContractStartedWithNestedContract>()
            .From<ContractExtendedWithNestedContract>(b => b
                .Set(m => m.EndDate).To(e => e.NewEndDate))
            .ClearWith<ContractEndedWithNestedContract>());
}
```
