# Projection with children

Projections can manage hierarchical data by defining child collections. This allows you to build read models that contain arrays or lists of related data, enabling one-to-many relationships within a single projection.

## Understanding children in Chronicle

In event sourcing, a parent entity often has a collection of related child items. Children in Chronicle projections allow you to:

- Manage collections of related items within a single read model
- Add, update, and remove individual items in the collection
- Maintain the parent-child relationship through events
- Join child data with other streams for enrichment

### Key concepts

- **Parent stream**: The main event stream identified by the projection's event source ID
- **Child identifier**: A unique property within each child that distinguishes it from other children in the collection
- **Child key**: The value from events that matches the child identifier to determine which child to create/update
- **Parent key**: When events occur on the parent stream but need routing to children

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

Understanding the mechanics of children collections is essential for building effective hierarchical projections:

### 1. Parent stream and projection instance

The parent projection is created from events on the parent stream:

```csharp
// When GroupCreated event occurs on stream "group-123"
// A Group read model is created with Id = "group-123"
.From<GroupCreated>()
```

### 2. Child identification with IdentifiedBy

`IdentifiedBy()` specifies which property uniquely identifies each child in the collection:

```csharp
.Children(m => m.Members, children => children
    .IdentifiedBy(e => e.UserId))  // Each child is identified by UserId
```

**Critical insight**: The `IdentifiedBy` property determines:
- Which child to update when an event arrives
- Which child to remove when a removal event occurs
- The uniqueness constraint (only one child per identifier value)

### 3. Routing events to children with UsingKey

`UsingKey()` extracts the child identifier from the event to route it to the correct child:

```csharp
// When UserAddedToGroup event occurs on stream "group-123"
// Event contains: { UserId: "user-456", Role: "Admin" }
.From<UserAddedToGroup>(b => b
    .UsingKey(e => e.UserId))  // Extract "user-456" from the event
// Chronicle finds or creates a child with UserId = "user-456"
// The child is added to the Members collection of Group "group-123"
```

### 4. The complete flow for children

Here's how events flow through a parent-child projection:

1. `GroupCreated` (stream: group-123, data: {Name: "Developers"}) → Creates Group with Id = "group-123", Members = []
2. `UserAddedToGroup` (stream: group-123, data: {UserId: "user-456", Role: "Admin"}) → Adds child to Members with UserId = "user-456"
3. `UserRoleChanged` (stream: group-123, data: {UserId: "user-456", NewRole: "Owner"}) → Finds child with UserId = "user-456" and updates it
4. `UserAddedToGroup` (stream: group-123, data: {UserId: "user-789", Role: "Member"}) → Adds another child with UserId = "user-789"
5. `UserRemovedFromGroup` (stream: group-123, data: {UserId: "user-456"}) → Removes child with UserId = "user-456"

**Important**: All these events occur on the **parent's stream** (group-123). The child identifier (UserId) is extracted from the event data, not from a different stream's event source ID.

## Understanding keys in children

### UsingKey vs UsingParentKey

There are two key methods for routing events to children:

**`UsingKey(e => e.ChildId)`** - Use when events occur on the parent stream:
```csharp
// Event on stream "group-123"
.From<UserAddedToGroup>(b => b
    .UsingKey(e => e.UserId))  // Route to child identified by e.UserId
```

**`UsingParentKey(e => e.ParentId)`** - Use when events occur on a different stream but need to route to a specific parent:
```csharp
// Event on stream "user-456" (not the group's stream!)
.From<UserJoinedGroup>(b => b
    .UsingParentKey(e => e.GroupId)  // Route to parent Group with Id = e.GroupId
    .UsingKey(e => e.UserId))        // Then to child identified by e.UserId
```

### Example: Different event sources

```csharp
// Scenario 1: Events on parent stream (group-123)
public class GroupProjectionWithParentEvents : IProjectionFor<Group>
{
    public void Define(IProjectionBuilderFor<Group> builder) => builder
        .From<GroupCreated>()
        .Children(m => m.Members, children => children
            .IdentifiedBy(e => e.UserId)
            .From<UserAddedToGroup>(b => b
                .UsingKey(e => e.UserId)));  // Event on group stream
}

// Scenario 2: Events on user stream (user-456) 
public class UserProjectionWithGroupChildren : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .From<UserCreated>()
        .Children(m => m.Groups, children => children
            .IdentifiedBy(e => e.GroupId)
            .From<UserJoinedGroup>(b => b
                .UsingParentKey(e => e.UserId)   // Route to User with Id = e.UserId
                .UsingKey(e => e.GroupId)));     // Add/update child identified by GroupId
}
```

### When to use which approach

**Use UsingKey only** (events on parent stream):
- Events naturally occur on the parent entity's stream
- Example: Adding a line item to an order (event on order stream)
- Example: Adding a member to a group (event on group stream)

**Use UsingParentKey + UsingKey** (events on different stream):
- Events occur on a related entity's stream
- You need to route to the correct parent first
- Example: User joining a group (event on user stream, but updates group list in User projection)
- Example: Project assignment (event on assignment stream, needs to update both user and project projections)

## Child lifecycle

## Child lifecycle

Children in a collection go through distinct lifecycle stages based on events:

### Adding children

When a child event arrives with a **new** identifier (not seen before in this collection):

```csharp
// First UserAddedToGroup event for user-456
// Members collection is empty []
await eventLog.Append("group-123", new UserAddedToGroup("user-456", "Admin"));
// Chronicle creates a new child: { UserId: "user-456", Role: "Admin" }
// Members collection becomes [{ UserId: "user-456", Role: "Admin" }]
```

**What happens**:
1. Chronicle checks if a child with `UserId = "user-456"` exists in Members
2. Not found → Creates a new GroupMember object
3. Applies AutoMap or explicit property mappings
4. Adds the child to the collection

### Updating children

When a child event arrives with an **existing** identifier:

```csharp
// Second event for the same user
await eventLog.Append("group-123", new UserRoleChanged("user-456", "Owner"));
// Chronicle finds existing child with UserId = "user-456"
// Updates: { UserId: "user-456", Role: "Owner" }
```

**What happens**:
1. Chronicle finds the existing child with `UserId = "user-456"`
2. Applies the property changes (updates Role to "Owner")
3. Other properties remain unchanged

### Removing children

Use `RemovedWith<>()` to specify which events remove children:

```csharp
.RemovedWith<UserRemovedFromGroup>(e => e.UserId)
```

When the event occurs:

```csharp
await eventLog.Append("group-123", new UserRemovedFromGroup("user-456"));
// Chronicle finds child with UserId = "user-456"
// Removes it from the collection
```

**What happens**:
1. Chronicle extracts the identifier from `e.UserId` → "user-456"
2. Finds the child with `UserId = "user-456"` in the Members collection
3. Removes the child from the collection
4. If not found, the event is ignored (idempotent)

### Child uniqueness

Only one child per identifier can exist in a collection:

```csharp
// These events will update the SAME child, not create multiple children
await eventLog.Append("group-123", new UserAddedToGroup("user-456", "Member"));
await eventLog.Append("group-123", new UserAddedToGroup("user-456", "Admin"));  // Updates existing child
// Result: One child with UserId = "user-456", Role = "Admin"
```

This is enforced by the `IdentifiedBy()` property.

## Removing children

The `RemovedWith<>()` method specifies how to remove child items from collections:

```csharp
.Children(m => m.Members, children => children
    .IdentifiedBy(e => e.UserId)
    .From<UserAddedToGroup>(b => b.UsingKey(e => e.UserId))
    .RemovedWith<UserRemovedFromGroup>(e => e.UserId))
```

### How RemovedWith works

When a `UserRemovedFromGroup` event is processed:

1. Chronicle extracts the identifier from the specified expression: `e.UserId`
2. Looks up a child with that identifier in the collection
3. If found, removes the child
4. If not found, ignores the event (making it idempotent)

### Multiple removal events

You can specify multiple events that trigger child removal:

```csharp
.Children(m => m.Sessions, children => children
    .IdentifiedBy(e => e.SessionId)
    .From<SessionStarted>(b => b.UsingKey(e => e.SessionId))
    .RemovedWith<SessionEnded>(e => e.SessionId)
    .RemovedWith<SessionExpired>(e => e.SessionId)
    .RemovedWith<SessionRevoked>(e => e.SessionId))
```

Different events can remove children based on different business conditions.

### Conditional removal

If you need more complex removal logic, you can use event properties:

```csharp
.RemovedWith<MembershipStatusChanged>(e => e.UserId)
// The event handler will remove the child when this event occurs
// You control which events to append based on your business logic
```

## Enriching children with joins

Children can be enriched with data from other streams using joins:

```csharp
public class DepartmentProjection : IProjectionFor<Department>
{
    public void Define(IProjectionBuilderFor<Department> builder) => builder
        .From<DepartmentCreated>()
        .Children(m => m.Employees, children => children
            .IdentifiedBy(e => e.EmployeeId)
            .From<EmployeeAssignedToDepartment>(b => b
                .UsingKey(e => e.EmployeeId)
                .Set(m => m.AssignedDate).ToEventContextProperty(c => c.Occurred))
            .Join<EmployeeHired>(j => j
                .Set(m => m.Name).To(e => e.Name)
                .Set(m => m.Email).To(e => e.Email))
            .Join<EmployeePromoted>(j => j
                .Set(m => m.Title).To(e => e.NewTitle)));
}
```

### How joins work with children

**Setting up the child**:
1. `EmployeeAssignedToDepartment` event occurs on stream "department-42"
2. Event contains `EmployeeId = "emp-123"`
3. A child is created with `EmployeeId = "emp-123"`

**Joining to enrich the child**:
4. When `EmployeeHired` event occurs on stream "emp-123"
5. Chronicle finds all children where `EmployeeId == "emp-123"`
6. Updates those children with Name and Email from the EmployeeHired event

**Key understanding**: 
- The child's identifier (`EmployeeId`) must match the event source ID of the joined stream
- `EmployeeHired` events have event source ID = "emp-123"
- The join links these automatically through the `EmployeeId` property

No explicit `On()` clause needed when joining children - Chronicle automatically uses the child's identifier (specified in `IdentifiedBy()`) as the join key.

### Bi-directional child relationships

You can model the same relationship from both perspectives:

```csharp
// From User perspective: Which groups does a user belong to?
public class UserGroupsProjection : IProjectionFor<UserProfile>
{
    public void Define(IProjectionBuilderFor<UserProfile> builder) => builder
        .From<UserCreated>()
        .Children(m => m.Groups, children => children
            .IdentifiedBy(e => e.GroupId)
            .From<UserJoinedGroup>(b => b
                .UsingKey(e => e.GroupId))
            .Join<GroupCreated>(j => j
                .Set(m => m.GroupName).To(e => e.Name)));
}

// From Group perspective: Which users are members of a group?
public class GroupMembersProjection : IProjectionFor<GroupDetail>
{
    public void Define(IProjectionBuilderFor<GroupDetail> builder) => builder
        .From<GroupCreated>()
        .Children(m => m.Members, children => children
            .IdentifiedBy(e => e.UserId)
            .From<UserJoinedGroup>(b => b
                .UsingParentKey(e => e.GroupId)  // Route to correct group
                .UsingKey(e => e.UserId))
            .Join<UserCreated>(j => j
                .Set(m => m.UserName).To(e => e.Name)));
}
```

**Key difference**:
- **UserGroupsProjection**: Events on user stream, children identified by GroupId
- **GroupMembersProjection**: Events on user stream but routed to group via `UsingParentKey`, children identified by UserId

## Multiple child collections

## Multiple child collections

A single projection can manage multiple independent child collections:

```csharp
public class ProjectProjection : IProjectionFor<Project>
{
    public void Define(IProjectionBuilderFor<Project> builder) => builder
        .From<ProjectCreated>()
        .Children(m => m.TeamMembers, children => children
            .IdentifiedBy(e => e.UserId)
            .From<DeveloperAssignedToProject>(b => b
                .UsingKey(e => e.UserId))
            .Join<UserCreated>(j => j
                .Set(m => m.Name).To(e => e.Name)))
        .Children(m => m.Tasks, children => children
            .IdentifiedBy(e => e.TaskId)
            .From<TaskAddedToProject>(b => b
                .UsingKey(e => e.TaskId))
            .From<TaskCompleted>(b => b
                .UsingKey(e => e.TaskId))
            .RemovedWith<TaskDeleted>(e => e.TaskId));
}
```

**Each child collection**:
- Has its own identifier property (`UserId` vs `TaskId`)
- Responds to its own set of events
- Can have its own joins
- Operates independently from other collections

### Nested children (children of children)

Chronicle supports complex hierarchies by nesting child collections:

```csharp
public class OrganizationProjection : IProjectionFor<Organization>
{
    public void Define(IProjectionBuilderFor<Organization> builder) => builder
        .From<OrganizationCreated>()
        .Children(m => m.Departments, children => children
            .IdentifiedBy(e => e.DepartmentId)
            .From<DepartmentAddedToOrganization>(b => b
                .UsingKey(e => e.DepartmentId))
            .Children(m => m.Teams, teams => teams
                .IdentifiedBy(e => e.TeamId)
                .From<TeamAddedToDepartment>(b => b
                    .UsingKey(e => e.TeamId))
                .Join<TeamCreated>(j => j
                    .Set(m => m.TeamName).To(e => e.Name))));
}
```

This creates a three-level hierarchy:
- Organization (root)
  - Departments (children)
    - Teams (children of children)

## Advanced patterns

### Children with composite keys

Children can use composite keys for identification:

```csharp
.Children(m => m.Allocations, children => children
    .IdentifiedBy(e => e.CompositeId)  // Property of type AllocationKey
    .From<ResourceAllocated>(b => b
        .UsingCompositeKey<AllocationKey>(k => k
            .Set(k => k.ResourceId).To(e => e.ResourceId)
            .Set(k => k.Month).To(e => e.AllocationMonth))))
```

The composite key uniquely identifies each child:

```csharp
public record AllocationKey(string ResourceId, string Month);

public record ResourceAllocation(
    AllocationKey CompositeId,
    decimal Hours,
    decimal Rate);
```

### Children from event properties

Sometimes the entire child object comes from a single event property:

```csharp
.Children(m => m.LineItems, children => children
    .IdentifiedBy(e => e.LineItemId)
    .FromEventProperty<OrderLineAdded>(e => e.LineItem))
```

This is useful when events carry complete child objects:

```csharp
[EventType]
public record OrderLineAdded(string LineItemId, OrderLine LineItem);

public record OrderLine(
    string LineItemId,
    string ProductId,
    int Quantity,
    decimal Price);
```

### Children with AutoMap

AutoMap works with children too, mapping event properties to child properties automatically:

```csharp
.Children(m => m.Members, children => children
    .IdentifiedBy(e => e.UserId)
    .AutoMap()
    .From<UserAddedToGroup>(b => b
        .UsingKey(e => e.UserId)))
```

This automatically maps properties like `UserId`, `Role`, etc., when property names match between the event and child read model.

## Common patterns and best practices

### ✅ Good: Clear child identification

```csharp
.Children(m => m.Members, children => children
    .IdentifiedBy(e => e.UserId)     // Clear, unique identifier
    .From<UserAddedToGroup>(b => b
        .UsingKey(e => e.UserId)))   // Matches IdentifiedBy
```

### ❌ Anti-pattern: Missing identifier

```csharp
.Children(m => m.Members, children => children
    // ❌ Missing: No IdentifiedBy()
    .From<UserAddedToGroup>(b => b
        .UsingKey(e => e.UserId)))   // Won't work without IdentifiedBy
```

### ✅ Good: Explicit removal events

```csharp
.Children(m => m.Sessions, children => children
    .IdentifiedBy(e => e.SessionId)
    .From<SessionStarted>(b => b.UsingKey(e => e.SessionId))
    .RemovedWith<SessionEnded>(e => e.SessionId))  // Clear removal
```

### ❌ Anti-pattern: No removal mechanism

```csharp
.Children(m => m.Sessions, children => children
    .IdentifiedBy(e => e.SessionId)
    .From<SessionStarted>(b => b.UsingKey(e => e.SessionId)))
    // ❌ Missing: No RemovedWith - children never removed
```

### ✅ Good: Children with joins for enrichment

```csharp
.Children(m => m.Orders, children => children
    .IdentifiedBy(e => e.OrderId)
    .From<OrderPlaced>(b => b
        .UsingKey(e => e.OrderId)
        .Set(m => m.CustomerId).To(e => e.CustomerId))
    .Join<CustomerCreated>(j => j
        .On(m => m.CustomerId)    // Enrich with customer data
        .Set(m => m.CustomerName).To(e => e.Name)))
```

### ✅ Good: Use UsingParentKey when events are on different streams

```csharp
// Event is on user stream, but updates group's children
.From<UserJoinedGroup>(b => b
    .UsingParentKey(e => e.GroupId)  // Route to correct parent
    .UsingKey(e => e.UserId))        // Then to correct child
```

## Performance considerations

### Child collection size

- Consider the expected size of child collections
- Very large collections (1000+ items) may impact query performance
- Consider pagination or separate projections for large datasets

### Join fan-out in children

- Joins on children can have significant fan-out
- Example: If 100 departments each have 50 employees, updating an employee affects 1 child in 1 projection
- But updating a department affects 50 children in that projection

### Update frequency

- Frequent updates to children trigger collection modifications
- Chronicle handles this efficiently, but consider the update patterns
- Batch related changes into single events when possible

Children enable powerful hierarchical read models that maintain parent-child relationships through events, with support for joins, removals, and complex nesting patterns.

