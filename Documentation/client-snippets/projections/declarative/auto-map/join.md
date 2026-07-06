```csharp title="AutoMap with a join"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record AutoMapEmployeeHired(string EmployeeName, string DepartmentId);

[EventType]
public record AutoMapDepartmentRenamed(string DepartmentName);

public record AutoMapEmployee(
    string EmployeeName,
    string DepartmentId,
    string DepartmentName);

public class AutoMapEmployeeProjection : IProjectionFor<AutoMapEmployee>
{
    public void Define(IProjectionBuilderFor<AutoMapEmployee> builder) => builder
        .From<AutoMapEmployeeHired>()
        .Join<AutoMapDepartmentRenamed>(_ => _
            .On(m => m.DepartmentId));
}
```
