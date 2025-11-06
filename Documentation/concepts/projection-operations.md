# Projection Operations

This document explains the complex operations available in Chronicle projections, focusing on how they work together to build sophisticated read models from event streams.

## Overview

Chronicle projections support several powerful operations that enable you to build read models from multiple event streams:

- **Joins**: Combine data from different event streams based on relationships
- **Children**: Manage hierarchical one-to-many relationships within a projection
- **Composite keys**: Use multiple properties to uniquely identify projections
- **Event context**: Leverage event metadata in your read models

These operations can be combined to create rich, denormalized read models that serve your application's query needs.

## Fundamental concepts

### Event streams and event source IDs

In Chronicle, events are organized into **streams**. Each stream represents the lifecycle of a single entity and is identified by an **event source ID**:

```csharp
// All events for user "user-123" belong to the user-123 stream
await eventLog.Append("user-123", new UserCreated("Alice"));
await eventLog.Append("user-123", new UserEmailChanged("alice@example.com"));

// All events for group "group-456" belong to the group-456 stream
await eventLog.Append("group-456", new GroupCreated("Admins"));
await eventLog.Append("group-456", new GroupDescriptionChanged("System administrators"));
```

Key principles:
- **Stream = Entity**: One stream per entity instance
- **Event source ID = Entity ID**: The unique identifier for the entity
- **Stream independence**: Each stream evolves independently
- **Temporal ordering**: Events within a stream are ordered by when they occurred

### Projections and read models

A **projection** is a specification for how to build a read model from events. The projection:

1. Watches for specific event types
2. Extracts data from those events
3. Updates read model instances accordingly

By default, a projection creates one read model instance per event source ID:

```csharp
// When UserCreated occurs on stream "user-123"
// A User read model is created with Id = "user-123"
public class UserProjection : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .From<UserCreated>(b => b
            .Set(m => m.Name).To(e => e.Name));
}
```

This one-to-one mapping (stream → read model instance) is the foundation. Joins and children extend this model to handle more complex scenarios.

## Joins: Cross-stream relationships

Joins allow a projection to incorporate data from multiple streams, creating denormalized read models that combine related information.

### How joins work

Joins rely on **storing a reference** to another stream's ID and then **watching for events** from that referenced stream:

```
┌─────────────────────────┐
│ User Stream (user-123)  │
├─────────────────────────┤
│ UserCreated             │──┐
│ UserAssignedToGroup     │  │ Creates User read model
│   GroupId: "group-456"  │  │ Sets GroupId = "group-456"
└─────────────────────────┘  │
                              │
                              ▼
                    ┌────────────────────┐
                    │ User Read Model    │
                    │ Id: "user-123"     │
                    │ GroupId: "group-456"│◄─┐
                    │ GroupName: ???     │  │
                    └────────────────────┘  │
                                            │ Join updates
                                            │ when group events occur
┌─────────────────────────┐                 │
│ Group Stream (group-456)│                 │
├─────────────────────────┤                 │
│ GroupCreated            │─────────────────┘
│   Name: "Administrators"│  Updates GroupName
└─────────────────────────┘
```

### Join mechanics

**Step 1: Establish the relationship**

An event sets a property to reference another stream's ID:

```csharp
.From<UserAssignedToGroup>(b => b
    .UsingKey(e => e.UserId)
    .Set(m => m.GroupId).ToEventSourceId())
```

This stores the group's stream ID in the User read model.

**Step 2: Define the join**

Specify which events from the referenced stream should update the read model:

```csharp
.Join<GroupCreated>(j => j
    .On(m => m.GroupId)  // Match on GroupId property
    .Set(m => m.GroupName).To(e => e.Name))
```

**Step 3: Chronicle maintains the relationship**

Chronicle creates an index: `GroupId value → List of User projection IDs`

When a `GroupCreated` event occurs on stream "group-456":
1. Chronicle looks up which User projections have `GroupId == "group-456"`
2. Updates the `GroupName` property on all matching User projections

### Join examples

**One-to-many relationship** (one group, many users):

```csharp
public class UserProjection : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .From<UserCreated>()
        .From<UserAssignedToGroup>(b => b
            .UsingKey(e => e.UserId)
            .Set(m => m.GroupId).ToEventSourceId())
        .Join<GroupCreated>(j => j
            .On(m => m.GroupId)
            .Set(m => m.GroupName).To(e => e.Name))
        .Join<GroupRenamed>(j => j
            .On(m => m.GroupId)
            .Set(m => m.GroupName).To(e => e.NewName));
}
```

**Multiple joins** (aggregating data from several streams):

```csharp
public class OrderProjection : IProjectionFor<OrderSummary>
{
    public void Define(IProjectionBuilderFor<OrderSummary> builder) => builder
        .From<OrderPlaced>(b => b
            .Set(m => m.CustomerId).To(e => e.CustomerId)
            .Set(m => m.ProductId).To(e => e.ProductId)
            .Set(m => m.ShipperId).To(e => e.ShipperId))
        .Join<CustomerCreated>(j => j
            .On(m => m.CustomerId)
            .Set(m => m.CustomerName).To(e => e.Name))
        .Join<ProductCreated>(j => j
            .On(m => m.ProductId)
            .Set(m => m.ProductName).To(e => e.Name)
            .Set(m => m.ProductPrice).To(e => e.Price))
        .Join<ShipperDetailsUpdated>(j => j
            .On(m => m.ShipperId)
            .Set(m => m.ShipperName).To(e => e.CompanyName));
}
```

This creates a denormalized view combining customer, product, and shipper information.

## Children: Hierarchical relationships

Children allow you to manage one-to-many relationships **within** a single projection, creating collections of related items.

### How children work

Children are managed through events that occur on the **parent's stream**:

```
┌──────────────────────────────┐
│ Group Stream (group-123)     │
├──────────────────────────────┤
│ GroupCreated                 │──┐
│ UserAddedToGroup             │  │ Creates Group read model
│   UserId: "user-456"         │  │ Adds child with UserId = "user-456"
│ UserAddedToGroup             │  │ Adds child with UserId = "user-789"
│   UserId: "user-789"         │  │
└──────────────────────────────┘  │
                                   ▼
                    ┌──────────────────────────────┐
                    │ Group Read Model             │
                    │ Id: "group-123"              │
                    │ Members: [                   │
                    │   { UserId: "user-456", ... },│
                    │   { UserId: "user-789", ... } │
                    │ ]                            │
                    └──────────────────────────────┘
```

### Child mechanics

**Step 1: Define the child collection**

Specify the property that holds the collection:

```csharp
.Children(m => m.Members, children => ...)
```

**Step 2: Specify child identifier**

Define which property uniquely identifies each child in the collection:

```csharp
.IdentifiedBy(e => e.UserId)
```

This ensures only one child per UserId exists in the collection.

**Step 3: Route events to children**

Extract the child identifier from events to route them to the correct child:

```csharp
.From<UserAddedToGroup>(b => b
    .UsingKey(e => e.UserId))
```

When this event occurs:
1. Chronicle extracts `UserId` from the event
2. Looks for a child with that `UserId` in the Members collection
3. If not found → creates a new child
4. If found → updates the existing child

### Child examples

**Basic child collection**:

```csharp
public class TeamProjection : IProjectionFor<Team>
{
    public void Define(IProjectionBuilderFor<Team> builder) => builder
        .From<TeamCreated>()
        .Children(m => m.Members, children => children
            .IdentifiedBy(e => e.UserId)
            .AutoMap()
            .From<MemberJoinedTeam>(b => b
                .UsingKey(e => e.UserId))
            .From<MemberRoleChanged>(b => b
                .UsingKey(e => e.UserId))
            .RemovedWith<MemberLeftTeam>(e => e.UserId));
}
```

**Multiple child collections**:

```csharp
public class ProjectProjection : IProjectionFor<Project>
{
    public void Define(IProjectionBuilderFor<Project> builder) => builder
        .From<ProjectCreated>()
        .Children(m => m.TeamMembers, children => children
            .IdentifiedBy(e => e.UserId)
            .From<DeveloperAssignedToProject>(b => b
                .UsingKey(e => e.UserId)))
        .Children(m => m.Milestones, children => children
            .IdentifiedBy(e => e.MilestoneId)
            .From<MilestoneAdded>(b => b
                .UsingKey(e => e.MilestoneId)));
}
```

## Combining joins and children

The real power comes from combining joins with children to create rich, denormalized views.

### Children with joins

Children can be enriched with data from other streams using joins:

```csharp
public class DepartmentProjection : IProjectionFor<Department>
{
    public void Define(IProjectionBuilderFor<Department> builder) => builder
        .From<DepartmentCreated>()
        .Children(m => m.Employees, children => children
            .IdentifiedBy(e => e.EmployeeId)
            .From<EmployeeAssignedToDepartment>(b => b
                .UsingKey(e => e.EmployeeId))
            .Join<EmployeeHired>(j => j
                .Set(m => m.Name).To(e => e.Name)
                .Set(m => m.Email).To(e => e.Email))
            .Join<EmployeePromoted>(j => j
                .Set(m => m.Title).To(e => e.NewTitle)));
}
```

**How this works**:

```
┌────────────────────────────────┐
│ Department Stream (dept-42)    │
├────────────────────────────────┤
│ DepartmentCreated              │──┐
│ EmployeeAssignedToDepartment   │  │ Creates Department read model
│   EmployeeId: "emp-123"        │  │ Adds child: { EmployeeId: "emp-123" }
└────────────────────────────────┘  │
                                     │
                                     ▼
                         ┌────────────────────────────┐
                         │ Department Read Model      │
                         │ Id: "dept-42"              │
                         │ Employees: [               │
                         │   {                        │
                         │     EmployeeId: "emp-123", │◄─┐
                         │     Name: ???,             │  │
                         │     Email: ???             │  │ Join enriches child
                         │   }                        │  │
                         │ ]                          │  │
                         └────────────────────────────┘  │
                                                          │
┌────────────────────────────────┐                       │
│ Employee Stream (emp-123)      │                       │
├────────────────────────────────┤                       │
│ EmployeeHired                  │───────────────────────┘
│   Name: "John Doe"             │  Updates child's Name and Email
│   Email: "john@example.com"    │
└────────────────────────────────┘
```

### Bi-directional relationships

Model many-to-many relationships from both perspectives:

```csharp
// Perspective 1: Users with their groups
public class UserGroupsProjection : IProjectionFor<UserWithGroups>
{
    public void Define(IProjectionBuilderFor<UserWithGroups> builder) => builder
        .From<UserCreated>()
        .Children(m => m.Groups, children => children
            .IdentifiedBy(e => e.GroupId)
            .From<UserJoinedGroup>(b => b
                .UsingKey(e => e.GroupId))
            .Join<GroupCreated>(j => j
                .Set(m => m.GroupName).To(e => e.Name)));
}

// Perspective 2: Groups with their members
public class GroupMembersProjection : IProjectionFor<GroupWithMembers>
{
    public void Define(IProjectionBuilderFor<GroupWithMembers> builder) => builder
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

**The difference**: In the second projection, `UserJoinedGroup` events occur on the **user stream**, not the group stream. Using `UsingParentKey(e => e.GroupId)` tells Chronicle to route the event to the Group projection with ID matching `e.GroupId`.

### Complex example: Project management

Here's a comprehensive example combining multiple concepts:

```csharp
public class ProjectDashboardProjection : IProjectionFor<ProjectDashboard>
{
    public void Define(IProjectionBuilderFor<ProjectDashboard> builder) => builder
        // Root project properties
        .From<ProjectCreated>(b => b
            .Set(m => m.Name).To(e => e.Name)
            .Set(m => m.OwnerId).To(e => e.OwnerId))
        
        // Join to enrich owner info
        .Join<UserCreated>(j => j
            .On(m => m.OwnerId)
            .Set(m => m.OwnerName).To(e => e.Name))
        
        // Team members as children with joins
        .Children(m => m.Team, children => children
            .IdentifiedBy(e => e.UserId)
            .From<DeveloperAssignedToProject>(b => b
                .UsingKey(e => e.UserId)
                .Set(m => m.Role).To(e => e.Role))
            .Join<UserCreated>(j => j
                .Set(m => m.Name).To(e => e.Name)
                .Set(m => m.Email).To(e => e.Email))
            .RemovedWith<DeveloperUnassignedFromProject>(e => e.UserId))
        
        // Tasks as children
        .Children(m => m.Tasks, children => children
            .IdentifiedBy(e => e.TaskId)
            .From<TaskAdded>(b => b
                .UsingKey(e => e.TaskId)
                .Set(m => m.AssignedToUserId).To(e => e.AssignedTo))
            .From<TaskCompleted>(b => b
                .UsingKey(e => e.TaskId)
                .Set(m => m.CompletedDate).ToEventContextProperty(c => c.Occurred))
            .Join<UserCreated>(j => j
                .On(m => m.AssignedToUserId)
                .Set(m => m.AssigneeName).To(e => e.Name))
            .RemovedWith<TaskDeleted>(e => e.TaskId));
}
```

This projection:
- Creates a read model per project stream
- Joins to the owner's user stream for owner details
- Has a Team child collection enriched with user data via joins
- Has a Tasks child collection where each task joins to its assignee's user stream
- Handles removal of team members and tasks

## Understanding keys across operations

Keys work differently depending on the context:

### Projection key (root level)

The projection instance ID, defaults to the event source ID:

```csharp
// Event on stream "proj-123" creates projection with Id = "proj-123"
.From<ProjectCreated>()
```

Can be customized with `UsingKey()` or `UsingCompositeKey()`:

```csharp
// Use a property from the event as the projection ID
.From<ProjectCreated>(b => b
    .UsingKey(e => e.ProjectCode))

// Use multiple properties as the projection ID
.From<OrderPlaced>(b => b
    .UsingCompositeKey<OrderKey>(k => k
        .Set(k => k.CustomerId).To(e => e.CustomerId)
        .Set(k => k.OrderNumber).To(e => e.OrderNumber)))
```

### Join key

A property in the read model that references another stream's ID:

```csharp
.From<UserAssignedToGroup>(b => b
    .Set(m => m.GroupId).ToEventSourceId())  // Store reference
.Join<GroupCreated>(j => j
    .On(m => m.GroupId))  // Use reference for joining
```

The value in `GroupId` must match the event source ID of group events.

### Child key

The property that uniquely identifies children within a collection:

```csharp
.Children(m => m.Members, children => children
    .IdentifiedBy(e => e.UserId)  // Child identifier property
    .From<UserAddedToGroup>(b => b
        .UsingKey(e => e.UserId)))  // Extract from event
```

Only one child per identifier value can exist in the collection.

### Parent key

When events occur on a different stream but need routing to a parent:

```csharp
// Event on user stream needs to update Group projection
.From<UserJoinedGroup>(b => b
    .UsingParentKey(e => e.GroupId)  // Route to Group with this ID
    .UsingKey(e => e.UserId))        // Then to child with this ID
```

## Performance and design considerations

### Join fan-out

When an event occurs on a joined stream, **all** projections that join to it are updated:

```
GroupRenamed event on "group-456"
  ↓
Updates ALL User projections where GroupId == "group-456"
  ↓
If 1000 users are in the group → 1000 projection updates
```

**Design tip**: Consider the direction of relationships based on your query patterns:
- If you often query "which users are in a group?", use children in GroupProjection
- If you often query "which groups does a user belong to?", use children in UserProjection
- You can have both, but it doubles the work when relationships change

### Update batching

Group related changes into single events:

```csharp
// ❌ Multiple events = multiple projection updates
await eventLog.Append(groupId, new GroupNameChanged(newName));
await eventLog.Append(groupId, new GroupDescriptionChanged(newDesc));

// ✅ Single event = one projection update
await eventLog.Append(groupId, new GroupDetailsUpdated(newName, newDesc));
```

### Index management

Chronicle automatically maintains indexes for:
- Join relationships: `join_key_value → [projection_ids]`
- Child identifiers: `parent_id + child_identifier → child_position`

These indexes enable fast lookups but consume storage. Design projections with this in mind.

### Projection specialization

Create multiple specialized projections rather than one giant projection:

```csharp
// ✅ Good: Specialized projections
public class UserSummaryProjection : IProjectionFor<UserSummary> { ... }  // For lists
public class UserDetailProjection : IProjectionFor<UserDetail> { ... }    // For detail views

// ❌ Avoid: One projection for everything
public class UserEverythingProjection : IProjectionFor<UserEverything> { ... }
```

Benefits:
- Better performance (only build what you need)
- Clearer intent (projection matches use case)
- Easier maintenance (changes affect specific use cases)

## Common patterns

### Denormalized lookups

Store IDs and denormalize names/descriptions for display:

```csharp
.From<TaskAssigned>(b => b
    .Set(m => m.ProjectId).To(e => e.ProjectId)      // Store ID
    .Set(m => m.AssigneeId).To(e => e.AssigneeId))   // Store ID
.Join<ProjectCreated>(j => j
    .On(m => m.ProjectId)
    .Set(m => m.ProjectName).To(e => e.Name))        // Denormalize name
.Join<UserCreated>(j => j
    .On(m => m.AssigneeId)
    .Set(m => m.AssigneeName).To(e => e.Name))       // Denormalize name
```

### Audit trails with children

Track history as a child collection:

```csharp
.Children(m => m.History, children => children
    .IdentifiedBy(e => e.Timestamp)
    .From<StatusChanged>(b => b
        .UsingCompositeKey<HistoryKey>(k => k
            .Set(k => k.Timestamp).ToEventContextProperty(c => c.Occurred)
            .Set(k => k.SequenceNumber).ToEventContextProperty(c => c.SequenceNumber))))
```

### Aggregations with children

Count or sum values across children:

```csharp
.Children(m => m.LineItems, children => children
    .IdentifiedBy(e => e.ItemId)
    .From<LineItemAdded>(b => b.UsingKey(e => e.ItemId)))
.From<LineItemAdded>(b => b
    .Count(m => m.TotalItems).Add(1))
.From<LineItemRemoved>(b => b
    .Count(m => m.TotalItems).Subtract(1))
```

## Summary

Chronicle's projection operations enable sophisticated read models:

- **Joins** connect data from multiple streams using stored references
- **Children** manage hierarchical relationships within a projection
- **Keys** (projection, join, child, parent) define how events are routed and data is linked
- **Combining operations** creates denormalized views optimized for queries

Understanding how these operations work together - particularly how keys and stream IDs interact - is essential for building effective projections that serve your application's needs while maintaining good performance.
