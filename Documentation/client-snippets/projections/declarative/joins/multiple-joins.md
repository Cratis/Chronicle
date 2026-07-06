```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecJoinsMultipleEmployeeAssigned(string GroupId, string DepartmentId, string LocationId);

[EventType]
public record DecJoinsMultipleGroupCreated(string Name);

[EventType]
public record DecJoinsMultipleDepartmentCreated(string Name);

[EventType]
public record DecJoinsMultipleLocationUpdated(string Address);

public record DecJoinsMultipleEmployeeSummary(
    string? GroupId,
    string? GroupName,
    string? DepartmentId,
    string? DepartmentName,
    string? LocationId,
    string? LocationAddress);

public class DecJoinsMultipleEmployeeSummaryProjection : IProjectionFor<DecJoinsMultipleEmployeeSummary>
{
    public void Define(IProjectionBuilderFor<DecJoinsMultipleEmployeeSummary> builder) => builder
        .AutoMap()
        .From<DecJoinsMultipleEmployeeAssigned>()
        .Join<DecJoinsMultipleGroupCreated>(j => j.On(m => m.GroupId))
        .Join<DecJoinsMultipleDepartmentCreated>(j => j.On(m => m.DepartmentId))
        .Join<DecJoinsMultipleLocationUpdated>(j => j.On(m => m.LocationId));
}
```
