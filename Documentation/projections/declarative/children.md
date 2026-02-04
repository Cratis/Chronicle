# Projection with children

Projections can manage hierarchical data by defining child collections. This allows you to build read models that contain arrays or lists of related data.

## Defining a projection with children

Use the `Children()` method to define child collections:

```csharp
using Cratis.Chronicle.Projections;

public class GroupProjection : IProjectionFor<Group>
{
    public void Define(IProjectionBuilderFor<Group> builder) => builder
        .AutoMap()
        .From<GroupCreated>()
        .Children(m => m.Members, children => children
            .IdentifiedBy(e => e.UserId)
            .AutoMap()
            .From<UserAddedToGroup>(b => b
                .UsingKey(e => e.UserId))
            .From<UserRoleChanged>(b => b
                .UsingKey(e => e.UserId))
            .RemovedWith<UserRemovedFromGroup>(e => e.UserId));
}

```

## Read model with children

The read model includes a collection property for the children:

```csharp
public record Group(
    string Name,
    string Description,
    IEnumerable<GroupMember> Members);

public record GroupMember(
    string UserId,
    string Role);

```

## Event definitions

Events that affect children use keys to identify which child to update:

```csharp
[EventType]
public record GroupCreated(string Name, string Description);

[EventType]
public record UserAddedToGroup(string UserId, string Role);

[EventType]
public record UserRoleChanged(string UserId, string NewRole);

[EventType]
public record UserRemovedFromGroup(string UserId);

```

## How children work

1. **Root events** (`GroupCreated`) update properties on the main read model
2. **Child events** (`UserAddedToGroup`, `UserRoleChanged`) are routed to child items
3. `IdentifiedBy()` specifies how to identify child items (by `UserId` in this example)
4. `UsingKey()` tells the projection which property contains the child identifier
5. Child items are created, updated, or remain unchanged based on the events

## Parent key resolution

By default, when a child event is processed, the framework uses the **EventSourceId** to identify the parent. This works well when the event is appended with the parent's identifier as the EventSourceId.

### Default behavior (EventSourceId as parent key)

In most scenarios, you don't need to specify the parent key explicitly:

```csharp
.Children(m => m.Members, children => children
    .IdentifiedBy(e => e.UserId)
    .AutoMap()
    .From<UserAddedToGroup>(b => b
        .UsingKey(e => e.UserId)))
    // No UsingParentKey needed - uses EventSourceId by default

```

When you append the event:


```csharp
await EventStore.EventLog.Append(groupId, new UserAddedToGroup(userId, role));

```

The `groupId` (EventSourceId) is automatically used to find the parent `Group`.

### Extracting parent key from event content

If your event contains the parent key as a property (instead of using EventSourceId), use `UsingParentKey()`:

```csharp
.Children(m => m.Members, children => children
    .IdentifiedBy(e => e.UserId)
    .AutoMap()
    .From<UserAddedToGroup>(b => b
        .UsingParentKey(e => e.GroupId)  // Extract from event content
        .UsingKey(e => e.UserId)))

```



When you append the event:

```csharp
await EventStore.EventLog.Append(userId, new UserAddedToGroup(userId, groupId, role));

```

The `groupId` property from the event content is used to find the parent `Group`.

### Using EventSourceId explicitly with UsingParentKeyFromContext

In some advanced scenarios, you might want to explicitly indicate that the EventSourceId should be used as the parent key (e.g., for documentation clarity):

```csharp
.Children(m => m.Members, children => children
    .IdentifiedBy(e => e.UserId)
    .AutoMap()
    .From<UserAddedToGroup>(b => b
        .UsingParentKeyFromContext(ctx => ctx.EventSourceId)  // Explicit
        .UsingKey(e => e.UserId)))

```

This is functionally equivalent to not specifying the parent key at all, but can make the intent clearer in complex projections.

### When to use each approach

- **No parent key specified** (default): Use when EventSourceId represents the parent identifier
- **`UsingParentKey(e => e.Property)`**: Use when parent identifier is in the event content
- **`UsingParentKeyFromContext(ctx => ctx.EventSourceId)`**: Use for explicit documentation of default behavior

## Child lifecycle

- **Adding children**: When a new event arrives with a previously unseen key, a new child is created
- **Updating children**: When an event arrives with an existing key, that child is updated
- **Removing children**: Use `RemovedWith<>()` to specify which events remove child items

## Removing children

The `RemovedWith<>()` method specifies how to remove child items from collections:

```csharp
.Children(m => m.Members, children => children
    .IdentifiedBy(e => e.UserId)
    .From<UserAddedToGroup>(_ => /* ... */)
    .RemovedWith<UserRemovedFromGroup>(e => e.UserId))

```

When a `UserRemovedFromGroup` event is processed:

1. The projection looks up the child using the specified key (`e.UserId`)
2. If found, the child is removed from the collection
3. If not found, the event is ignored

You can also remove children conditionally or based on other criteria by using multiple `RemovedWith<>()` calls.

## Multiple child collections

A single projection can have multiple child collections:

```csharp
.AutoMap()
.Children(m => m.Members, children => children
    .IdentifiedBy(e => e.UserId)
    .AutoMap()
    .From<UserAddedToGroup>(_ => /* ... */))
.Children(m => m.Tasks, children => children
    .IdentifiedBy(e => e.TaskId)
    .AutoMap()
    .From<TaskAssignedToGroup>(_ => /* ... */));

```

This pattern allows you to build rich, hierarchical read models from events.
