# Children

Children define nested collections within a projection. Each child represents a collection of items that are managed independently but belong to a parent projection.

## Basic Syntax

```
children {CollectionName} id {Identifier}
  {child blocks}
```

## Simple Example

```
projection Group => GroupReadModel
  from GroupCreated
    Name = name

  children members id userId
    from UserAddedToGroup key userId
      parent groupId
      Name = userName
      Role = role
```

This creates a `members` collection where each member is identified by `userId`.

## Identifier Expression

The `id` specifies how to identify individual children:

```
children orders id orderId
children members id userId
children items id itemNumber
```

## Parent Key

Use `parent` to specify the relationship to the parent projection:

```
children members id userId
  from UserAddedToGroup key userId
    parent groupId
    Name = userName
```

The `parent groupId` links the child to the correct parent instance.

## With AutoMap

Apply AutoMap to children:

```
children members id userId
  automap

  from UserAddedToGroup
    Role = role
```

## Multiple Events

Children can have multiple event types:

```
children members id userId
  from UserAddedToGroup key userId
    parent groupId
    Name = userName
    Role = role

  from UserRoleChanged key userId
    parent groupId
    Role = role
```

## Joins in Children

Children can have joins:

```
children members id userId
  from UserAddedToGroup key userId
    parent groupId
    UserId = userId
    Role = role

  join User on UserId
    events UserCreated, UserUpdated
    Name = name
    Email = email
```

## Removal in Children

Remove children when specific events occur:

```
children members id userId
  from UserAddedToGroup key userId
    parent groupId
    Role = role

  remove with UserRemovedFromGroup key userId
    parent groupId
```

See [Removal](removal.md) for more details.

## Nested Children

Children can contain their own children:

```
children departments id deptId
  from DepartmentCreated key deptId
    parent companyId
    Name = name

  children teams id teamId
    from TeamCreated key teamId
      parent deptId
      Name = name
```

## Examples

### Group Membership

```
projection Group => GroupReadModel
  from GroupCreated
    Name = name
    Description = description

  children members id userId
    automap

    from UserAddedToGroup key userId
      parent groupId
      AddedAt = $eventContext.occurred

    from UserRoleChanged key userId
      parent groupId
      Role = role

    remove with UserRemovedFromGroup key userId
      parent groupId
```

### Order with Line Items

```
projection Order => OrderReadModel
  from OrderPlaced
    OrderNumber = orderNumber
    CustomerId = customerId
    Total = 0

  children items id lineNumber
    from LineItemAdded key lineNumber
      parent orderId
      ProductId = productId
      Quantity = quantity
      UnitPrice = price
      LineTotal = total

    from LineItemQuantityChanged key lineNumber
      parent orderId
      Quantity = quantity
      LineTotal = total

    remove with LineItemRemoved key lineNumber
      parent orderId
```

### Project with Tasks

```
projection Project => ProjectReadModel
  from ProjectCreated
    Name = name
    Status = "Active"

  children tasks id taskId
    from TaskAdded key taskId
      parent projectId
      Title = title
      AssignedTo = assigneeId
      Status = "Open"
      CreatedAt = $eventContext.occurred

    from TaskCompleted key taskId
      parent projectId
      Status = "Completed"
      CompletedAt = $eventContext.occurred

    from TaskAssigned key taskId
      parent projectId
      AssignedTo = assigneeId

    join User on AssignedTo
      events UserCreated
      AssigneeName = name
```

### Invoice with Payments

```
projection Invoice => InvoiceReadModel
  from InvoiceIssued
    InvoiceNumber = number
    CustomerId = customerId
    Amount = amount
    Balance = amount

  children payments id paymentId
    from PaymentReceived key paymentId
      parent invoiceNumber
      Amount = amount
      ReceivedAt = $eventContext.occurred
      Method = paymentMethod

  from PaymentReceived
    subtract Balance by amount
```

### Blog Post with Comments

```
projection BlogPost => BlogPostReadModel
  from PostPublished
    Title = title
    Content = content
    AuthorId = authorId
    PublishedAt = $eventContext.occurred

  from PostUpdated
    Content = content
    UpdatedAt = $eventContext.occurred

  children comments id commentId
    from CommentAdded key commentId
      parent postId
      AuthorId = authorId
      Content = content
      CreatedAt = $eventContext.occurred

    from CommentEdited key commentId
      parent postId
      Content = content
      EditedAt = $eventContext.occurred

    remove with CommentDeleted key commentId
      parent postId

    join User on AuthorId
      events UserRegistered
      AuthorName = name
```

### Multi-Level Nesting

```
projection Company => CompanyReadModel
  from CompanyCreated
    Name = name

  children departments id deptId
    from DepartmentCreated key deptId
      parent companyId
      Name = name

    children teams id teamId
      from TeamCreated key teamId
        parent deptId
        Name = name

      children members id memberId
        from MemberAdded key memberId
          parent teamId
          Name = name
          Role = role
```

## Parent Key Expressions

The parent key can use various expressions:

```
children items id itemId
  from ItemAdded key itemId
    parent orderId                        # Event property
    # or
    parent $eventContext.eventSourceId    # Event source ID
    # or
    parent order.id                       # Nested property
```

## AutoMap with Children

AutoMap at the children level applies to all child events:

```
children members id userId
  automap

  from UserAdded key userId
    parent groupId
    # Name, Email, etc. are auto-mapped

  from UserUpdated key userId
    parent groupId
    # Updates are auto-mapped
```

## Collection Property

The children block maps to a collection property on the read model:

```csharp
public class GroupReadModel
{
    public string Name { get; set; }
    public List<GroupMember> Members { get; set; } = new();
}

public class GroupMember
{
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
}
```

## Best Practices

1. **Clear Identifiers**: Use meaningful identifiers that clearly identify children
2. **Parent Keys**: Always specify parent keys to maintain relationships
3. **Removal**: Include removal logic for children that can be deleted
4. **Joins for Enrichment**: Use joins to add related data to children
5. **Nested Carefully**: Deep nesting can impact performance and complexity
6. **AutoMap Selectively**: Use AutoMap for children when property names align
7. **Consistent Patterns**: Use consistent event patterns across children

## Common Patterns

### Add/Update/Remove

```
children items id itemId
  from ItemAdded key itemId
    parent parentId
    # properties

  from ItemUpdated key itemId
    parent parentId
    # updated properties

  remove with ItemRemoved key itemId
    parent parentId
```

### Enrichment with Joins

```
children members id userId
  from MemberAdded key userId
    parent groupId
    UserId = userId

  join User on UserId
    events UserCreated, UserUpdated
    Name = name
    Email = email
```

### Versioning

```
children versions id versionNumber
  from VersionCreated key versionNumber
    parent documentId
    Content = content
    CreatedAt = $eventContext.occurred
    CreatedBy = authorId
```

## See Also

- [Joins](joins.md) - Enriching children with related data
- [Removal](removal.md) - Removing children when events occur
- [Keys](keys.md) - Understanding child and parent keys
