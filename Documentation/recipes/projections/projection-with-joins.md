# Projection with joins

Joins allow projections to incorporate data from events that don't share the same event source ID. This enables building read models that combine data from different streams, creating powerful cross-stream relationships while maintaining proper stream boundaries.

## Understanding joins in Chronicle

In event sourcing, events are organized into streams identified by event source IDs. Each stream represents the lifecycle of a single entity. However, read models often need to display data from multiple related entities. Joins solve this problem by allowing projections to pull in data from other streams based on relationships established through events.

### Key concepts

- **Primary stream**: The main event stream that establishes the projection instance (using the event source ID)
- **Joined streams**: Additional event streams that provide related data
- **Join key**: A property in the read model that links to another stream's event source ID
- **Event source ID**: The unique identifier for an event stream (the "key" of the stream)

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

Understanding the mechanics of joins is crucial for building effective projections:

### 1. Establishing the projection instance

The projection instance is created when a primary event occurs. The event source ID of this event becomes the ID of the read model:

```csharp
// When UserCreated event with event source ID "user-123" occurs:
// - A User read model with Id "user-123" is created
.From<UserCreated>()
```

### 2. Setting join keys

Join keys link the read model to other streams. Typically, you set a join key from an event that establishes the relationship:

```csharp
// When UserAssignedToGroup event occurs on stream "user-123":
// - The event has UserId = "user-123" and GroupId = "group-456"
// - We set m.GroupId to the event source ID of the GROUP stream
.From<UserAssignedToGroup>(b => b
    .UsingKey(e => e.UserId)              // Process events for this user
    .Set(m => m.GroupId).ToEventSourceId()) // Store the group's stream ID
```

**Critical insight**: `ToEventSourceId()` doesn't get the event source ID from the UserAssignedToGroup event. Instead, it means "this property will hold an event source ID value from another stream" - in this case, the value from `e.GroupId` in the event.

### 3. Joining on the key

Once a join key is set, `Join<>()` watches for events from the stream identified by that key:

```csharp
// Chronicle watches for GroupCreated events
// When a GroupCreated event occurs on stream "group-456":
// - Chronicle checks all User projections where GroupId == "group-456"
// - Those User projections are updated with data from the GroupCreated event
.Join<GroupCreated>(j => j
    .On(m => m.GroupId))  // Link: event source ID of GroupCreated == m.GroupId
```

### 4. The complete flow

Here's how the events flow through the system:

1. `UserCreated` (stream: user-123) → Creates User read model with Id = "user-123"
2. `UserAssignedToGroup` (stream: user-123, data: {UserId: "user-123", GroupId: "group-456"}) → Sets User.GroupId = "group-456"
3. `GroupCreated` (stream: group-456, data: {Name: "Administrators"}) → Updates User read model where GroupId == "group-456" with group data
4. `GroupRenamed` (stream: group-456, data: {NewName: "Super Admins"}) → Updates User read model again with new group name

**The join mechanism**: Chronicle maintains an index that tracks "which User projections have GroupId == group-456" so when group events occur, it knows exactly which users to update.

## Understanding streams and keys

### Event streams and event source IDs

Every event in Chronicle belongs to a stream, identified by an **event source ID**. Think of it as:

- **Stream** = All events for a single entity (e.g., all events for User "user-123")
- **Event source ID** = The unique identifier for that entity/stream (e.g., "user-123")

When you append an event, you specify which stream it belongs to:

```csharp
// This appends to the "user-123" stream
await eventLog.Append("user-123", new UserCreated("John Doe", "john@example.com"));

// This appends to the "group-456" stream
await eventLog.Append("group-456", new GroupCreated("Administrators", "System admins"));
```

### Joins connect streams through stored relationships

Joins work by:

1. **Storing a reference** to another stream's ID in your read model
2. **Listening for events** from that referenced stream
3. **Updating the read model** when those events occur

The join key is not just any property - it must contain a value that corresponds to an event source ID of another stream.

### Example: Library system

Consider a library system with books and borrowers:

```csharp
// Book stream events (stream ID = ISBN)
await eventLog.Append("978-0-13-468599-1", new BookAddedToInventory("Clean Code", "Robert Martin"));

// Borrower stream events (stream ID = borrower ID)
await eventLog.Append("borrower-42", new BorrowerRegistered("Alice Smith"));

// Borrowing event (stream ID = transaction ID, but contains both book and borrower IDs)
await eventLog.Append("txn-789", new BookBorrowed("978-0-13-468599-1", "borrower-42"));
```

The projection combines data from all three streams:

```csharp
public class BorrowedBookProjection : IProjectionFor<BorrowedBook>
{
    public void Define(IProjectionBuilderFor<BorrowedBook> builder) => builder
        .From<BookBorrowed>(b => b
            .Set(m => m.BookId).To(e => e.BookId)      // Store book stream ID
            .Set(m => m.BorrowerId).To(e => e.BorrowerId)) // Store borrower stream ID
        .Join<BookAddedToInventory>(j => j
            .On(m => m.BookId)                          // When book events occur
            .Set(m => m.BookTitle).To(e => e.Title))
        .Join<BorrowerRegistered>(j => j
            .On(m => m.BorrowerId)                      // When borrower events occur
            .Set(m => m.BorrowerName).To(e => e.Name));
}
```

When `BookAddedToInventory` event occurs on stream "978-0-13-468599-1":
- Chronicle finds all BorrowedBook projections where `BookId == "978-0-13-468599-1"`
- Updates their `BookTitle` property

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

### Multiple joins on the same projection

A projection can join with multiple streams to aggregate data from various sources:

```csharp
public class EmployeeProjection : IProjectionFor<Employee>
{
    public void Define(IProjectionBuilderFor<Employee> builder) => builder
        .AutoMap()
        .From<EmployeeHired>(b => b
            .Set(m => m.DepartmentId).To(e => e.DepartmentId)
            .Set(m => m.ManagerId).To(e => e.ManagerId)
            .Set(m => m.LocationId).To(e => e.LocationId))
        .Join<DepartmentCreated>(j => j
            .On(m => m.DepartmentId)
            .Set(m => m.DepartmentName).To(e => e.Name))
        .Join<EmployeeHired>(j => j  // Join to another employee's stream
            .On(m => m.ManagerId)
            .Set(m => m.ManagerName).To(e => e.Name))
        .Join<LocationOpened>(j => j
            .On(m => m.LocationId)
            .Set(m => m.LocationName).To(e => e.Name)
            .Set(m => m.LocationCity).To(e => e.City));
}
```

This creates a rich read model that combines data from:
- The employee's own stream (using event source ID)
- The department stream (via DepartmentId)
- Another employee's stream for manager info (via ManagerId)
- The location stream (via LocationId)

### Updating joined data

When events occur on joined streams, the projection automatically updates:

```csharp
// Department stream event
await eventLog.Append("dept-sales", new DepartmentRenamed("Sales & Marketing"));

// This automatically updates ALL Employee projections where DepartmentId == "dept-sales"
// You don't need to specify which employees - Chronicle's join index handles this
```

### Joining children

Joins can also be used within child collections to enrich child items with data from other streams:

```csharp
public class DepartmentProjection : IProjectionFor<Department>
{
    public void Define(IProjectionBuilderFor<Department> builder) => builder
        .AutoMap()
        .From<DepartmentCreated>()
        .Children(m => m.Employees, children => children
            .IdentifiedBy(e => e.EmployeeId)
            .From<EmployeeAssignedToDepartment>(b => b
                .UsingKey(e => e.EmployeeId)  // Child key, not parent key
                .Set(m => m.AssignedDate).ToEventContextProperty(c => c.Occurred))
            .Join<EmployeeHired>(j => j
                .Set(m => m.EmployeeName).To(e => e.Name)
                .Set(m => m.Email).To(e => e.Email))
            .Join<EmployeePromoted>(j => j
                .Set(m => m.Title).To(e => e.NewTitle)));
}
```

**How it works for children**:

1. `EmployeeAssignedToDepartment` event occurs on the department's stream
2. The event contains `EmployeeId = "emp-123"`
3. A child is added to the Employees collection with `EmployeeId = "emp-123"`
4. When `EmployeeHired` or `EmployeePromoted` events occur on stream "emp-123"
5. Chronicle finds the child in the collection where `EmployeeId == "emp-123"` and updates it

**Important**: The join on children uses the child's identifier (EmployeeId), not the parent's identifier (DepartmentId). The child's identifier must match the event source ID of the joined stream.

## Advanced join patterns

### Bi-directional relationships

You can model many-to-many relationships by creating projections on both sides:

```csharp
// User side: Shows which groups a user belongs to
public class UserGroupsProjection : IProjectionFor<UserWithGroups>
{
    public void Define(IProjectionBuilderFor<UserWithGroups> builder) => builder
        .From<UserCreated>(b => b.Set(m => m.Name).To(e => e.Name))
        .Children(m => m.Groups, children => children
            .IdentifiedBy(e => e.GroupId)
            .From<UserJoinedGroup>(b => b
                .UsingKey(e => e.GroupId))
            .Join<GroupCreated>(j => j
                .Set(m => m.GroupName).To(e => e.Name)));
}

// Group side: Shows which users belong to a group
public class GroupMembersProjection : IProjectionFor<GroupWithMembers>
{
    public void Define(IProjectionBuilderFor<GroupWithMembers> builder) => builder
        .From<GroupCreated>(b => b.Set(m => m.Name).To(e => e.Name))
        .Children(m => m.Members, children => children
            .IdentifiedBy(e => e.UserId)
            .From<UserJoinedGroup>(b => b
                .UsingParentKey(e => e.GroupId)  // Note: UsingParentKey for group stream
                .UsingKey(e => e.UserId))        // Child identified by UserId
            .Join<UserCreated>(j => j
                .Set(m => m.UserName).To(e => e.Name)));
}
```

**Key differences**:
- **UserGroupsProjection**: Events on user stream (using `UsingKey(e => e.GroupId)` for child)
- **GroupMembersProjection**: Events on group stream (using `UsingParentKey(e => e.GroupId)` to route to correct parent)

The `UsingParentKey()` method tells Chronicle which property contains the parent read model's ID, allowing the same event type to be processed correctly in both projections.

### Join with conditional property updates

Joins can update multiple properties and use different events for different properties:

```csharp
public class OrderProjection : IProjectionFor<Order>
{
    public void Define(IProjectionBuilderFor<Order> builder) => builder
        .AutoMap()
        .From<OrderPlaced>(b => b
            .Set(m => m.CustomerId).To(e => e.CustomerId)
            .Set(m => m.ProductId).To(e => e.ProductId))
        .Join<CustomerCreated>(j => j
            .On(m => m.CustomerId)
            .Set(m => m.CustomerName).To(e => e.Name)
            .Set(m => m.CustomerEmail).To(e => e.Email))
        .Join<CustomerAddressChanged>(j => j
            .On(m => m.CustomerId)
            .Set(m => m.ShippingAddress).To(e => e.NewAddress))
        .Join<ProductCreated>(j => j
            .On(m => m.ProductId)
            .Set(m => m.ProductName).To(e => e.Name)
            .Set(m => m.ProductPrice).To(e => e.Price))
        .Join<ProductPriceChanged>(j => j
            .On(m => m.ProductId)
            .Set(m => m.ProductPrice).To(e => e.NewPrice));
}
```

Different events from the same stream can update different properties:
- `CustomerCreated` sets name and email
- `CustomerAddressChanged` only updates the address
- `ProductPriceChanged` only updates the price

Chronicle processes each event type independently based on the join configuration.

### Joins with AutoMap

When using `AutoMap()` in a join, Chronicle automatically maps properties with matching names:

```csharp
.Join<GroupCreated>(j => j
    .On(m => m.GroupId)
    .AutoMap())  // Automatically maps matching property names
```

This is useful when the joined event has many properties that match your read model. You can combine AutoMap with explicit property mappings:

```csharp
.Join<GroupCreated>(j => j
    .On(m => m.GroupId)
    .AutoMap()  // Maps Name, Description, etc.
    .Set(m => m.GroupCreatedDate).ToEventContextProperty(c => c.Occurred))  // Add context property
```

## Performance considerations

## Performance considerations

### Join indexing

- Chronicle automatically creates and maintains indexes for join relationships
- When a joined event occurs, Chronicle uses these indexes to find all affected projections
- The index structure is optimized for fast lookups: `{join_key_value} -> [list of projection IDs]`

### Update propagation

- When an event occurs on a joined stream, **all** projections joined to that stream are updated
- For example, if 1000 users belong to a group, a `GroupRenamed` event updates 1000 User projections
- Consider the "fan-out" factor: how many projections typically join to a single stream

### Best practices for performance

**Group related updates**: If you frequently update multiple properties from the same stream, use a single event rather than multiple events:

```csharp
// ❌ Not optimal: Multiple events cause multiple projection updates
await eventLog.Append(groupId, new GroupNameChanged(newName));
await eventLog.Append(groupId, new GroupDescriptionChanged(newDesc));
await eventLog.Append(groupId, new GroupIconChanged(newIcon));

// ✅ Better: Single event updates all properties at once
await eventLog.Append(groupId, new GroupDetailsUpdated(newName, newDesc, newIcon));
```

**Consider join cardinality**: Understand the relationship multiplicity:
- **One-to-many** (1 group → 100 users): Updates to the group affect many projections
- **Many-to-one** (100 orders → 1 customer): Updates to a single order affect only one projection
- Design your projections based on which direction you query most frequently

**Use passive projections for rare queries**: If you only occasionally need joined data, consider using separate projections:

```csharp
// Active projection without joins for common queries
public class UserSummaryProjection : IProjectionFor<UserSummary> { ... }

// Passive projection with joins for detailed views
[Passive]
public class UserDetailProjection : IProjectionFor<UserDetail> { ... }
```

**Limit join depth**: Avoid "joins of joins" where possible:
```csharp
// ❌ Avoid: User → Group → Department → Location (4-level relationship)
// ✅ Better: User → Group with Department info already in Group projection
```

### Monitoring join performance

Monitor these metrics in production:
- **Join fan-out**: How many projections are updated per joined event
- **Update latency**: Time taken to update all projections after a joined event
- **Join index size**: Storage used for maintaining join relationships

Chronicle handles join indexes automatically, but understanding these factors helps you design efficient projections.

## Common patterns and anti-patterns

### ✅ Good: Store reference, join for enrichment

```csharp
.From<OrderPlaced>(b => b
    .Set(m => m.CustomerId).To(e => e.CustomerId))  // Store the reference
.Join<CustomerCreated>(j => j
    .On(m => m.CustomerId)
    .Set(m => m.CustomerName).To(e => e.Name))      // Enrich with denormalized data
```

### ❌ Anti-pattern: Join without storing reference

```csharp
// ❌ Missing: No property to store CustomerId
.From<OrderPlaced>()
.Join<CustomerCreated>(j => j
    .On(m => m.CustomerId))  // Error: CustomerId not set anywhere
```

### ✅ Good: Multiple joins for different relationships

```csharp
.From<TaskAssigned>(b => b
    .Set(m => m.ProjectId).To(e => e.ProjectId)
    .Set(m => m.AssigneeId).To(e => e.AssigneeId))
.Join<ProjectCreated>(j => j.On(m => m.ProjectId))
.Join<UserCreated>(j => j.On(m => m.AssigneeId))
```

### ❌ Anti-pattern: Storing entire objects instead of IDs

```csharp
// ❌ Don't store complex objects as join keys
.Set(m => m.Group).To(e => e.CompleteGroupObject)  // Wrong: Store ID, not object

// ✅ Store the ID, join for the data
.Set(m => m.GroupId).To(e => e.GroupId)
.Join<GroupCreated>(j => j.On(m => m.GroupId))
```

Joins enable powerful cross-stream read models while maintaining the benefits of event sourcing and proper stream boundaries. Understanding how keys and stream IDs work together is essential for building efficient and maintainable projections.

