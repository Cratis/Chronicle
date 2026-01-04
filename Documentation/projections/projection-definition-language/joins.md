# Joins

Joins allow you to enrich a projection with data from related events that share a common key. This is useful when you need to combine information from different event streams.

## Basic Syntax

```
join {Name} on {Property}
  events {EventType}, {EventType}, ...
  {mappings}
```

## Simple Example

```
projection Order => OrderReadModel
  from OrderPlaced
    OrderNumber = orderNumber
    CustomerId = customerId

  join Customer on CustomerId
    events CustomerCreated, CustomerUpdated
    CustomerName = name
    CustomerEmail = email
```

When a `CustomerCreated` or `CustomerUpdated` event occurs with a matching `CustomerId`, the projection updates with customer information.

## With AutoMap

Automatically map matching properties from joined events:

```
join Group on GroupId
  events GroupCreated, GroupRenamed
  automap
```

## Multiple Mappings

Apply multiple mappings within a join:

```
join Product on ProductId
  events ProductCreated, ProductUpdated
  ProductName = name
  ProductDescription = description
  ProductPrice = price
  LastProductUpdate = $eventContext.occurred
```

## Multiple Events

Join multiple event types that share the same key:

```
join Customer on CustomerId
  events CustomerRegistered, CustomerUpdated, CustomerVerified
  CustomerName = name
  IsVerified = verified
```

## Join with From Events

Combine joins with regular from events:

```
projection Order => OrderReadModel
  from OrderPlaced
    OrderNumber = orderNumber
    CustomerId = customerId
    Total = total

  from OrderShipped
    ShippedAt = $eventContext.occurred
    Status = "Shipped"

  join Customer on CustomerId
    events CustomerCreated, CustomerUpdated
    CustomerName = name
```

## Multiple Joins

A projection can have multiple joins:

```
projection Order => OrderReadModel
  from OrderPlaced
    OrderNumber = orderNumber
    CustomerId = customerId
    ProductId = productId

  join Customer on CustomerId
    events CustomerCreated
    CustomerName = name

  join Product on ProductId
    events ProductCreated
    ProductName = name
    ProductPrice = price
```

## Joins in Children

Joins can be used within children blocks:

```
projection Group => GroupReadModel
  from GroupCreated
    Name = name

  children members identified by userId
    from UserAddedToGroup
      UserId = userId
      Role = role

    join User on UserId
      events UserCreated, UserUpdated
      UserName = name
      UserEmail = email
```

## Examples

### Order with Customer Details

```
projection Order => OrderReadModel
  from OrderPlaced
    OrderNumber = orderNumber
    CustomerId = customerId
    Total = total
    PlacedAt = $eventContext.occurred

  join Customer on CustomerId
    events CustomerCreated, CustomerUpdated
    automap
```

### Product with Category Information

```
projection Product => ProductReadModel
  from ProductCreated
    ProductId = productId
    Name = name
    CategoryId = categoryId

  join Category on CategoryId
    events CategoryCreated, CategoryRenamed
    CategoryName = name
    CategoryDescription = description
```

### User Profile with Organization

```
projection UserProfile => UserProfileReadModel
  from ProfileCreated
    UserId = userId
    Name = name
    OrganizationId = organizationId

  from ProfileUpdated
    Bio = bio
    Avatar = avatarUrl

  join Organization on OrganizationId
    events OrganizationCreated, OrganizationRenamed
    OrganizationName = name
    OrganizationType = type
```

### Task with Assignee Details

```
projection Task => TaskReadModel
  from TaskCreated
    Title = title
    Description = description
    AssignedTo = assigneeId

  from TaskAssigned
    AssignedTo = assigneeId
    AssignedAt = $eventContext.occurred

  join User on AssignedTo
    events UserRegistered, UserUpdated
    AssigneeName = name
    AssigneeEmail = email
```

### Reservation with Room and Guest

```
projection Reservation => ReservationReadModel
  from ReservationMade
    ReservationNumber = number
    RoomId = roomId
    GuestId = guestId
    CheckIn = checkInDate
    CheckOut = checkOutDate

  join Room on RoomId
    events RoomCreated, RoomUpdated
    RoomNumber = number
    RoomType = type
    RoomFloor = floor

  join Guest on GuestId
    events GuestRegistered, GuestUpdated
    GuestName = name
    GuestEmail = email
    GuestPhone = phone
```

### Children with Join

```
projection Project => ProjectReadModel
  from ProjectCreated
    Name = name
    ManagerId = managerId

  join User on ManagerId
    events UserCreated
    ManagerName = name

  children tasks identified by taskId
    from TaskAdded
      Title = title
      AssignedTo = assigneeId

    join User on AssignedTo
      events UserCreated, UserUpdated
      AssigneeName = name
```

## How Joins Work

1. **Event Occurs**: A joined event is appended to the event store
2. **Key Matching**: Chronicle finds projections where the join key matches
3. **Update**: The projection is updated with data from the joined event
4. **Multiple Events**: All specified events trigger updates

## Join Key Requirements

- The join key property must exist on the read model
- The join key value from events must match the read model's property value
- Multiple projections can join on the same event stream

## Best Practices

1. **Explicit Events**: List all event types that should update the join
2. **AutoMap for Matching**: Use AutoMap when property names align
3. **Specific Mappings**: Override AutoMap with explicit mappings as needed
4. **Multiple Joins**: Use multiple joins to combine data from various sources
5. **Performance**: Joins can impact performance; consider denormalization needs
6. **Event Ordering**: Be aware that joined events may arrive in any order

## Common Patterns

### Reference Data

Join with reference data like categories, types, or statuses:

```
join Status on StatusId
  events StatusCreated, StatusUpdated
  StatusName = name
  StatusColor = color
```

### User Information

Enrich with user details:

```
join User on UserId
  events UserRegistered, UserProfileUpdated
  UserName = name
  UserAvatar = avatarUrl
```

### Nested Relationships

Join within children for nested enrichment:

```
children items identified by itemId
  from ItemAdded
    ProductId = productId

  join Product on ProductId
    events ProductCreated
    ProductName = name
```

## See Also

- [Children](children.md) - Nested collections with joins
- [Removal](removal.md) - Removing projections based on joined events
- [Keys](keys.md) - Understanding join key matching
