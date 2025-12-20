# Projection with RemoveWithJoin

The `RemovedWithJoin<>()` method allows you to remove child items from collections when events from other streams indicate that related data should be removed. This is particularly useful in scenarios where the removal event occurs on a different stream than the one that originally added the child.

## Understanding RemoveWithJoin vs RemovedWith

- **`RemovedWith<>()`**: Removes children based on events from the same stream
- **`RemovedWithJoin<>()`**: Removes children based on events from different streams (joins)

## Basic RemoveWithJoin usage

Use `RemovedWithJoin<>()` in child projections to specify which events should trigger child removal:

```csharp
public class UserProjection : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .AutoMap()
        .From<UserCreated>()
        .Children(m => m.Groups, children => children
            .IdentifiedBy(e => e.GroupId)
            .AutoMap()
            .From<UserAddedToGroup>(_ => _
                .UsingParentKey(e => e.UserId)
                .Set(m => m.JoinedAt).ToEventContextProperty(c => c.Occurred))
            .Join<GroupCreated>(_ => _
                .On(m => m.GroupId))
            .RemovedWithJoin<GroupDeleted>());
}
```

In this example:

- When a `UserAddedToGroup` event occurs, a group is added to the user's collection
- `UsingParentKey(e => e.UserId)` extracts the parent (user) identifier from the event content
- When a `GroupDeleted` event occurs anywhere in the system, that group is removed from all users
- The removal is based on the group ID that was used to join the data

> **Note**: If you don't specify `UsingParentKey()`, the framework uses the EventSourceId as the parent identifier by default. Use `UsingParentKey()` when the parent identifier is a property in the event content rather than the EventSourceId.

## How RemoveWithJoin works

When using `RemovedWithJoin<>()`:

1. **Event occurs**: The specified event type (e.g., `GroupDeleted`) is processed
2. **Key extraction**: The system extracts the key from the event source ID or specified key expression
3. **Child lookup**: All projections with children joined on that key are found
4. **Removal**: The matching child items are removed from all affected collections

## RemoveWithJoin with explicit keys

You can specify which property to use as the key for removal:

```csharp
.Children(m => m.Projects, children => children
    .IdentifiedBy(e => e.ProjectId)
    .AutoMap()
    .From<EmployeeAssignedToProject>(_ => _
        .UsingParentKey(e => e.EmployeeId)
        .UsingKey(e => e.ProjectId)
        .Set(m => m.AssignedAt).ToEventContextProperty(c => c.Occurred))
    .Join<ProjectCreated>(_ => _
        .On(m => m.ProjectId))
    .RemovedWithJoin<ProjectCancelled>(_ => _
        .UsingKey(e => e.ProjectId)))
```

## Real-world example: User groups

Consider a system where users can be members of groups, and groups can be deleted:

```csharp
public class GroupMembershipProjection : IProjectionFor<UserProfile>
{
    public void Define(IProjectionBuilderFor<UserProfile> builder) => builder
        .AutoMap()
        .From<UserRegistered>(_ => _
            .Set(m => m.RegisteredAt).ToEventContextProperty(c => c.Occurred))
        .Children(m => m.Memberships, children => children
            .IdentifiedBy(e => e.GroupId)
            .AutoMap()
            .From<UserJoinedGroup>(_ => _
                .UsingParentKey(e => e.UserId)
                .UsingKey(e => e.GroupId)
                .Set(m => m.JoinedAt).ToEventContextProperty(c => c.Occurred))
            .Join<GroupCreated>(_ => _
                .On(m => m.GroupId))
            .RemovedWith<UserLeftGroup>(_ => _
                .UsingParentKey(e => e.UserId)
                .UsingKey(e => e.GroupId))
            .RemovedWithJoin<GroupDisbanded>());
}
```

In this example:

- **`RemovedWith<UserLeftGroup>`**: Removes membership when a user explicitly leaves a group
- **`RemovedWithJoin<GroupDisbanded>`**: Removes membership from all users when a group is disbanded

## Complex scenario: Project assignments

```csharp
public class DeveloperProjectsProjection : IProjectionFor<DeveloperProfile>
{
    public void Define(IProjectionBuilderFor<DeveloperProfile> builder) => builder
        .AutoMap()
        .From<DeveloperOnboarded>(_ => _
            .Set(m => m.OnboardedAt).ToEventContextProperty(c => c.Occurred))
        .Children(m => m.CurrentProjects, children => children
            .IdentifiedBy(e => e.ProjectId)
            .AutoMap()
            .From<DeveloperAssignedToProject>(_ => _
                .UsingParentKey(e => e.DeveloperId)
                .UsingKey(e => e.ProjectId)
                .Set(m => m.AssignedAt).ToEventContextProperty(c => c.Occurred))
            .Join<ProjectInitiated>(_ => _
                .On(m => m.ProjectId))
            .RemovedWith<DeveloperUnassignedFromProject>(_ => _
                .UsingParentKey(e => e.DeveloperId)
                .UsingKey(e => e.ProjectId))
            .RemovedWithJoin<ProjectCancelled>()
            .RemovedWithJoin<ProjectCompleted>());
}
```

This handles three removal scenarios:

1. **Individual unassignment**: `DeveloperUnassignedFromProject` removes one developer from one project
2. **Project cancellation**: `ProjectCancelled` removes the project from all developers
3. **Project completion**: `ProjectCompleted` removes the project from all developers

## Read model examples

```csharp
public record UserProfile(
    string UserId,
    string Username,
    string Email,
    DateTimeOffset RegisteredAt,
    IEnumerable<GroupMembership> Memberships);

public record GroupMembership(
    string GroupId,
    string GroupName,
    string GroupType,
    DateTimeOffset JoinedAt,
    string Role);

public record DeveloperProfile(
    string DeveloperId,
    string Name,
    IEnumerable<string> Skills,
    DateTimeOffset OnboardedAt,
    IEnumerable<ProjectAssignment> CurrentProjects);

public record ProjectAssignment(
    string ProjectId,
    string ProjectName,
    string Priority,
    DateTimeOffset Deadline,
    DateTimeOffset AssignedAt,
    string Role,
    int Allocation);
```

## Event definitions

```csharp
[EventType]
public record UserRegistered(string Username, string Email);

[EventType]
public record UserJoinedGroup(string UserId, string GroupId, string Role);

[EventType]
public record UserLeftGroup(string UserId, string GroupId);

[EventType]
public record GroupCreated(string Name, string Type);

[EventType]
public record GroupDisbanded();

[EventType]
public record DeveloperAssignedToProject(string DeveloperId, string ProjectId, string Role, int AllocationPercentage);

[EventType]
public record DeveloperUnassignedFromProject(string DeveloperId, string ProjectId);

[EventType]
public record ProjectInitiated(string Name, string Priority, DateTimeOffset Deadline);

[EventType]
public record ProjectCancelled();

[EventType]
public record ProjectCompleted();
```

## Best practices

### Use for cross-stream cleanup

`RemovedWithJoin<>()` is ideal when:

- Events from one stream should trigger cleanup in projections of other stream
- You need to maintain referential integrity across stream boundaries
- Deletion or deactivation events should cascade to related read models

### Combine with RemovedWith

Often you'll use both removal methods together:

- `RemovedWith<>()` for explicit removals within the same stream
- `RemovedWithJoin<>()` for cascade removals from related streams

### Consider event ordering

Be aware that `RemovedWithJoin<>()` events might be processed before all related `Join<>()` events have completed, so ensure your system handles partial data gracefully.

The `RemovedWithJoin<>()` method provides powerful cross-stream cleanup capabilities, ensuring that your read models stay consistent when related data is removed from other parts of your system.
