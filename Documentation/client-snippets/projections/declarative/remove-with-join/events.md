```csharp
using Cratis.Chronicle.Events;

[EventType]
public record DecRemoveWithJoinUserRegistered(string Username, string Email);

[EventType]
public record DecRemoveWithJoinUserJoinedGroup(string UserId, string GroupId, string Role);

[EventType]
public record DecRemoveWithJoinUserLeftGroup(string UserId, string GroupId);

[EventType]
public record DecRemoveWithJoinGroupCreated(string GroupName, string GroupType);

[EventType]
public record DecRemoveWithJoinGroupDisbanded;

[EventType]
public record DecRemoveWithJoinDeveloperOnboarded(string Name, IEnumerable<string> Skills);

[EventType]
public record DecRemoveWithJoinDeveloperAssignedToProject(string DeveloperId, string ProjectId, string Role, int Allocation);

[EventType]
public record DecRemoveWithJoinDeveloperUnassignedFromProject(string DeveloperId, string ProjectId);

[EventType]
public record DecRemoveWithJoinProjectInitiated(string ProjectName, string Priority, DateTimeOffset Deadline);

[EventType]
public record DecRemoveWithJoinProjectCancelled;

[EventType]
public record DecRemoveWithJoinProjectCompleted;
```
