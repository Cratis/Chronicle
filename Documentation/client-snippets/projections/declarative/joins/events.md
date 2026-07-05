```csharp
using Cratis.Chronicle.Events;

// User stream events
[EventType]
public record DecJoinsUserCreated(string Name, string Email);

[EventType]
public record DecJoinsUserAssignedToGroup(string UserId, string GroupId);

// Group stream events
[EventType]
public record DecJoinsGroupCreated(string Name, string Description);

[EventType]
public record DecJoinsGroupRenamed(string NewName);
```
