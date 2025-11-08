# Projection with joins

Joins allow projections to incorporate data from events that don't share the same event source ID. This enables building read models that combine data from different streams.

## Defining a projection with joins

Use the `Join()` method to include data from events with different keys:

```csharp
using Cratis.Chronicle.Projections;

public class UserProjection : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .AutoMap()
        .From<UserCreated>()
        .From<UserAssignedToGroup>(b => b
            .UsingKey(e => e.UserId)
            .Set(m => m.GroupId).ToEventSourceId())
        .Join<GroupCreated>(j => j
            .On(m => m.GroupId))
        .Join<GroupRenamed>(j => j
            .On(m => m.GroupId));
}
```

## Read model with joined data

The read model includes properties populated from different event sources:

```csharp
public record User(
    string Name,
    string Email,
    string? GroupId,
    string? GroupName,
    string? GroupDescription);
```

## Event definitions

Events come from different streams but are joined based on common identifiers:

```csharp
// User stream events
[EventType]
public record UserCreated(string Name, string Email);

[EventType]
public record UserAssignedToGroup(string UserId, string GroupId);

// Group stream events
[EventType]
public record GroupCreated(string Name, string Description);

[EventType]
public record GroupRenamed(string NewName);
```

## How joins work

1. **Primary events** establish the read model and may set join keys
2. **Join conditions** specify which property links to other streams
3. **Joined events** update properties when their event source ID matches the join key
4. Join properties are updated whenever relevant events occur

## Join scenarios

### Setting join keys

Join keys are typically set from events that establish relationships:

```csharp
.From<UserAssignedToGroup>(b => b
    .UsingKey(e => e.UserId)
    .Set(m => m.GroupId).ToEventSourceId())  // Sets join key to group's event source ID
```

### Joining on the key

Joins match events based on their event source ID and the join property:

```csharp
.AutoMap()
.Join<GroupCreated>(j => j
    .On(m => m.GroupId))  // Join condition: group events where eventSourceId == m.GroupId
```

### Multiple joins

A projection can join with multiple streams:

```csharp
.AutoMap()
.Join<GroupCreated>(j => j.On(m => m.GroupId))
.Join<DepartmentCreated>(j => j.On(m => m.DepartmentId))
.Join<LocationUpdated>(j => j.On(m => m.LocationId));
```

### Joining children

Joins can also be used within child collections:

```csharp
.Children(m => m.Tasks, children => children
    .IdentifiedBy(e => e.TaskId)
    .AutoMap()
    .From<TaskAssigned>(b => b
        .UsingKey(e => e.TaskId))
    .Join<ProjectCreated>(j => j
        .On(m => m.ProjectId)));
```

## Performance considerations

- Joins require Chronicle to track relationships between streams
- The system automatically manages join indexes and updates
- Consider the frequency of joined events when designing projections
- Large numbers of joins may impact projection performance

Joins enable powerful cross-stream read models while maintaining the benefits of event sourcing and proper stream boundaries.
