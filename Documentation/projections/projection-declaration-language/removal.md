# Removal

Removal allows you to delete projection instances when specific events occur. Chronicle supports two types of removal: direct removal (`remove with`) and removal based on joined events (`remove via join`).

## Remove With

Remove a projection instance when a specific event occurs:

```pdl
remove with {EventType}
```pdl

### Basic Example

```pdl
projection User => UserReadModel
  from UserCreated
    Name = name
    Email = email

  remove with UserDeleted
```pdl

When a `UserDeleted` event occurs, the entire `UserReadModel` instance is removed.

### With Explicit Key

Specify which property from the event identifies the instance to remove:

```pdl
projection User => UserReadModel
  from UserCreated key userId
    Name = name

  remove with UserDeleted key userId
```pdl

### In Children

Remove individual children:

```pdl
children members identified by userId
  from UserAddedToGroup key userId
    parent groupId
    Role = role

  remove with UserRemovedFromGroup key userId
    parent groupId
```pdl

The `parent` key ensures the correct child is removed from the correct parent.

## Remove Via Join

Remove a projection instance when a joined event occurs:

```pdl
remove via join on {EventType}
```pdl

### Basic Example

```pdl
projection UserProfile => UserProfileReadModel
  from ProfileCreated
    UserId = userId

  join User on UserId
    events UserCreated
    Name = name

  remove via join on UserDeleted
```pdl

When a `UserDeleted` event occurs, any `UserProfileReadModel` instances with matching `UserId` are removed.

### With Key

```pdl
remove via join on UserDeleted key userId
```pdl

### In Children

Remove children when a joined event occurs:

```pdl
children groups identified by groupId
  from UserAddedToGroup
    parent userId
    GroupId = groupId

  join Group on GroupId
    events GroupCreated, GroupRenamed
    Name = name

  remove via join on GroupDeleted
```pdl

When a `GroupDeleted` event occurs, the child group is removed from the user's groups collection.

## Examples

### User Account Deletion

```pdl
projection User => UserReadModel
  from UserRegistered
    Name = name
    Email = email
    CreatedAt = $eventContext.occurred

  from UserEmailChanged
    Email = email
    UpdatedAt = $eventContext.occurred

  remove with UserDeleted
```pdl

### Group with Member Removal

```pdl
projection Group => GroupReadModel
  from GroupCreated
    Name = name

  children members identified by userId
    from UserAddedToGroup key userId
      parent groupId
      Name = userName
      Role = role

    from UserRoleChanged key userId
      parent groupId
      Role = role

    remove with UserRemovedFromGroup key userId
      parent groupId
```pdl

### Product with Category Removal

```pdl
projection Product => ProductReadModel
  from ProductCreated
    Name = name
    CategoryId = categoryId

  join Category on CategoryId
    events CategoryCreated
    CategoryName = name

  remove via join on CategoryDeleted
```pdl

When the category is deleted, all products in that category are removed.

### Order Line Item Removal

```pdl
projection Order => OrderReadModel
  from OrderPlaced
    OrderNumber = orderNumber
    Total = 0

  children items identified by lineNumber
    from LineItemAdded key lineNumber
      parent orderId
      ProductId = productId
      Quantity = quantity
      LineTotal = total

    from LineItemQuantityChanged key lineNumber
      parent orderId
      Quantity = quantity

    remove with LineItemRemoved key lineNumber
      parent orderId

  from LineItemAdded
    add Total by total

  from LineItemRemoved
    subtract Total by total
```pdl

### User Profile with Account Deletion

```pdl
projection UserProfile => UserProfileReadModel
  from ProfileCreated
    UserId = userId
    Bio = bio
    Avatar = avatarUrl

  from ProfileUpdated
    Bio = bio
    UpdatedAt = $eventContext.occurred

  join User on UserId
    events UserRegistered, UserUpdated
    Name = name
    Email = email

  remove via join on UserAccountDeleted
```pdl

### Subscription with Plan Removal

```pdl
projection Subscription => SubscriptionReadModel
  from SubscriptionCreated
    UserId = userId
    PlanId = planId
    StartDate = startDate

  join SubscriptionPlan on PlanId
    events PlanCreated, PlanUpdated
    PlanName = name
    Price = price

  remove with SubscriptionCancelled

  remove via join on PlanDiscontinued
```pdl

Both `SubscriptionCancelled` and `PlanDiscontinued` will remove the subscription.

### Task with Project Deletion

```pdl
projection Task => TaskReadModel
  from TaskCreated
    Title = title
    ProjectId = projectId
    AssignedTo = assigneeId

  join Project on ProjectId
    events ProjectCreated
    ProjectName = name

  join User on AssignedTo
    events UserCreated
    AssigneeName = name

  remove via join on ProjectDeleted
```pdl

When the project is deleted, all associated tasks are removed.

### Multi-Level Removal

```pdl
projection Department => DepartmentReadModel
  from DepartmentCreated
    Name = name
    CompanyId = companyId

  join Company on CompanyId
    events CompanyCreated
    CompanyName = name

  children employees identified by employeeId
    from EmployeeAssigned key employeeId
      parent deptId
      Name = name

    remove with EmployeeUnassigned key employeeId
      parent deptId

  remove with DepartmentDisbanded

  remove via join on CompanyDissolved
```pdl

Employees can be removed individually, the department can be disbanded, or everything is removed if the company is dissolved.

## Parent Keys in Child Removal

When removing children, the parent key ensures the correct instance:

```pdl
remove with ChildRemoved key childId
  parent parentId
```pdl

This removes the child with `childId` from the parent identified by `parentId`.

## Key Matching

Removal finds instances to delete by matching keys:

- **Projection-level**: Uses the projection's key (explicit or event source ID)
- **Children-level**: Uses the child identifier and parent key
- **Join-based**: Uses the join key to find matching instances

## Multiple Removal Conditions

A projection can have multiple removal conditions:

```pdl
projection Document => DocumentReadModel
  from DocumentCreated
    Title = title
    OwnerId = ownerId

  join User on OwnerId
    events UserCreated
    OwnerName = name

  remove with DocumentDeleted
  remove via join on UserAccountClosed
```pdl

The document is removed if either it's deleted or the owner's account is closed.

## Best Practices

1. **Explicit Keys**: Specify keys for precise removal
2. **Parent Keys in Children**: Always include parent keys when removing children
3. **Multiple Conditions**: Use both direct and join-based removal when appropriate
4. **Cascade Carefully**: Consider the impact of join-based removal on related data
5. **Audit Trail**: Consider keeping soft deletes rather than hard removal for audit purposes
6. **Event Ordering**: Be aware that removal events may arrive out of order

## Common Patterns

### Soft Delete Alternative

Instead of removing, mark as deleted:

```pdl
projection User => UserReadModel
  from UserCreated
    Name = name
    IsDeleted = false

  from UserDeleted
    IsDeleted = true
    DeletedAt = $eventContext.occurred
```pdl

### Cascade Delete

Remove related items when parent is removed:

```pdl
projection Order => OrderReadModel
  from OrderPlaced
    CustomerId = customerId

  join Customer on CustomerId
    events CustomerCreated
    CustomerName = name

  children items identified by itemId
    from ItemAdded key itemId
      parent orderId
      ProductId = productId

    remove via join on ProductDiscontinued

  remove via join on CustomerAccountClosed
```pdl

### Conditional Removal

Use multiple specific events:

```pdl
projection Subscription => SubscriptionReadModel
  from SubscriptionCreated
    Status = "Active"

  remove with SubscriptionCancelled
  remove with PaymentFailed
  remove with TrialExpired
```pdl

## Differences Between Remove With and Remove Via Join

| Feature | Remove With | Remove Via Join |
|---------|-------------|-----------------|
| Trigger | Direct event | Event from joined stream |
| Use Case | Explicit deletion | Related entity deletion |
| Key | Event key matches projection key | Join key matches |
| Scope | Direct control | Dependent on relationship |

## See Also

- [Joins](joins.md) - Understanding join relationships for removal
- [Children](children.md) - Removing children collections
- [Keys](keys.md) - Understanding key matching for removal
