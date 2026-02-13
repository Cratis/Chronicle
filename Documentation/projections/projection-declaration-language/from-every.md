# From Every

The `every` directive applies mappings to every event that affects the projection, regardless of event type. This is useful for setting common properties like last update timestamps.

## Basic Syntax

```pdl
every
  {mappings}
```

## Simple Example

```pdl
projection User => UserReadModel
  every
    LastUpdated = $eventContext.occurred
    UpdatedBy = $eventContext.eventSourceId

  from UserRegistered
    Name = name
    Email = email

  from UserEmailChanged
    Email = email
```

Both `UserRegistered` and `UserEmailChanged` events will set `LastUpdated` and `UpdatedBy`.

## With AutoMap

Apply AutoMap to all events:

```pdl
projection Product => ProductReadModel
  every
    automap
    LastModified = $eventContext.occurred
```

## Exclude Children

By default, `every` applies to events in child collections. To exclude them:

```pdl
every
  LastUpdated = $eventContext.occurred
  exclude children
```

This ensures child events don't update the parent's `LastUpdated` field.

## Common Patterns

### Audit Fields

Track when the projection was last modified:

```pdl
every
  LastModified = $eventContext.occurred
  LastSequenceNumber = $eventContext.sequenceNumber
  LastCorrelationId = $eventContext.correlationId
```

### Event Source Tracking

Store the event source that last modified the projection:

```pdl
every
  LastModifiedBy = $eventContext.eventSourceId
  LastModifiedAt = $eventContext.occurred
```

### Version Tracking

Track the latest event sequence:

```pdl
every
  Version = $eventContext.sequenceNumber
  UpdatedAt = $eventContext.occurred
```

## Examples

### User Profile with Audit

```pdl
projection User => UserReadModel
  every
    LastUpdated = $eventContext.occurred
    LastSequenceNumber = $eventContext.sequenceNumber
    exclude children

  from UserRegistered
    Name = name
    Email = email
    CreatedAt = $eventContext.occurred

  from UserEmailChanged
    Email = email

  from UserNameChanged
    Name = name
```

### Product with Global AutoMap

```pdl
projection Product => ProductReadModel
  every
    automap
    LastModified = $eventContext.occurred

  from ProductCreated
    IsActive = true

  from ProductDeactivated
    IsActive = false
```

### Document Versioning

```pdl
projection Document => DocumentReadModel
  every
    CurrentVersion = $eventContext.sequenceNumber
    LastUpdated = $eventContext.occurred
    LastCorrelation = $eventContext.correlationId
    exclude children

  from DocumentCreated
    Title = title
    Content = content

  from DocumentUpdated
    Content = content

  from DocumentRenamed
    Title = title
```

### Order with Tracking

```pdl
projection Order => OrderReadModel
  every
    LastEventTime = $eventContext.occurred
    EventCount = $eventContext.sequenceNumber
    exclude children

  from OrderPlaced
    CustomerId = customerId
    Total = total
    Status = "Placed"

  from OrderShipped
    ShippedAt = $eventContext.occurred
    Status = "Shipped"

  from OrderDelivered
    DeliveredAt = $eventContext.occurred
    Status = "Delivered"

  children items id itemId
    from ItemAdded
      ProductId = productId
      Quantity = quantity
```

## Scope and Application

### Parent Level Only (with exclude children)

```pdl
every
  LastUpdated = $eventContext.occurred
  exclude children
```

Parent events update `LastUpdated`, child events do not.

### Including Children (default)

```pdl
every
  LastUpdated = $eventContext.occurred
```

Both parent and child events update `LastUpdated`.

### With Multiple Children

```pdl
projection Group => GroupReadModel
  every
    LastActivity = $eventContext.occurred
    exclude children

  from GroupCreated
    Name = name

  children members id userId
    from UserAdded
      Name = userName

  children posts id postId
    from PostCreated
      Title = title
```

Group-level events update `LastActivity`, but member and post events do not.

## Best Practices

1. **Use for Common Fields**: Apply `every` for timestamps and audit fields that should update with any event
2. **Exclude Children**: Use `exclude children` when parent and child lifecycles are independent
3. **Keep It Simple**: Limit `every` to truly universal properties
4. **Combine with Specific Rules**: Use `from` events for specific updates, `every` for universal ones
5. **AutoMap Carefully**: Global AutoMap can have unintended effects; use selectively
6. **Version Tracking**: Sequence numbers are ideal for optimistic concurrency control

## When to Use

**Use `every` when:**
- Setting last modified timestamps
- Tracking latest sequence numbers
- Maintaining version counters
- Storing correlation IDs for all events
- Applying AutoMap universally

**Avoid `every` when:**
- Only specific events should update a property
- Different events require different logic
- Children should be treated independently
