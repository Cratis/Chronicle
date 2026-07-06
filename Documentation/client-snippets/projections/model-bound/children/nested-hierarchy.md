```csharp
// Events
[EventType]
public record MbChildrenNestedOrganizationCreated(string Name);

[EventType]
public record MbChildrenNestedDepartmentAdded(Guid Id, string Name);

[EventType]
public record MbChildrenNestedDepartmentRenamed(Guid Id, string NewName);

[EventType]
public record MbChildrenNestedTeamAdded(Guid Id, Guid DepartmentId, string Name);

[EventType]
public record MbChildrenNestedTeamRenamed(Guid Id, string NewName);

// Read Models - all attributes work at every nesting level
public record MbChildrenNestedOrganization(
    [Key] Guid Id,

    [SetFrom<MbChildrenNestedOrganizationCreated>]
    string Name,

    [ChildrenFrom<MbChildrenNestedDepartmentAdded>(
        key: nameof(MbChildrenNestedDepartmentAdded.Id),
        identifiedBy: nameof(MbChildrenNestedDepartment.Id))]
    IEnumerable<MbChildrenNestedDepartment> Departments);

public record MbChildrenNestedDepartment(
    [Key] Guid Id,

    [SetFrom<MbChildrenNestedDepartmentAdded>]
    [Join<MbChildrenNestedDepartmentRenamed>(eventPropertyName: nameof(MbChildrenNestedDepartmentRenamed.NewName))] // Joins work on children
    string Name,

    [ChildrenFrom<MbChildrenNestedTeamAdded>(
        key: nameof(MbChildrenNestedTeamAdded.Id),
        identifiedBy: nameof(MbChildrenNestedTeam.Id),
        parentKey: nameof(MbChildrenNestedTeamAdded.DepartmentId))] // Nested children
    IEnumerable<MbChildrenNestedTeam> Teams);

public record MbChildrenNestedTeam(
    [Key] Guid Id,

    [SetFrom<MbChildrenNestedTeamAdded>]
    [Join<MbChildrenNestedTeamRenamed>(eventPropertyName: nameof(MbChildrenNestedTeamRenamed.NewName))] // Joins work on nested children too
    string Name);
```
