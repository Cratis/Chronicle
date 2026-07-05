```csharp title="Child lifecycle events"
using Cratis.Chronicle.Events;

[EventType]
public record GroupCreatedForChildEvents(string Name, string Description);

[EventType]
public record UserAddedToGroupForChildEvents(string UserId, string Role);

[EventType]
public record UserRoleChangedForChildEvents(string UserId, string Role);

[EventType]
public record UserRemovedFromGroupForChildEvents(string UserId);
```
