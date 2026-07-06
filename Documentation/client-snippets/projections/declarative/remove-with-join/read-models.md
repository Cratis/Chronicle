```csharp
public record DecRemoveWithJoinUserProfile(
    string UserId,
    string Username,
    string Email,
    DateTimeOffset RegisteredAt,
    IEnumerable<DecRemoveWithJoinGroupMembership> Memberships);

public record DecRemoveWithJoinGroupMembership(
    string GroupId,
    string GroupName,
    string GroupType,
    DateTimeOffset JoinedAt,
    string Role);

public record DecRemoveWithJoinDeveloperProfile(
    string DeveloperId,
    string Name,
    IEnumerable<string> Skills,
    DateTimeOffset OnboardedAt,
    IEnumerable<DecRemoveWithJoinProjectAssignment> CurrentProjects);

public record DecRemoveWithJoinProjectAssignment(
    string ProjectId,
    string ProjectName,
    string Priority,
    DateTimeOffset Deadline,
    DateTimeOffset AssignedAt,
    string Role,
    int Allocation);
```
