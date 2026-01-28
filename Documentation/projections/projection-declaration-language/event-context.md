# Event Context

Event context provides access to metadata about the event itself, such as when it occurred, its sequence number, and correlation information.

## Syntax

Access event context properties using the `$eventContext.` prefix:

```
$eventContext.{property}
```

## Available Properties

### occurred

The timestamp when the event occurred:

```
from UserRegistered
  Name = name
  CreatedAt = $eventContext.occurred
```

### sequenceNumber

The sequence number of the event in its stream:

```
from EventProcessed
  LastSequenceNumber = $eventContext.sequenceNumber
```

### correlationId

The correlation ID associated with the event:

```
from OrderPlaced
  OrderId = orderId
  CorrelationId = $eventContext.correlationId
```

### eventSourceId

The event source ID (also available as shorthand `$eventSourceId`):

```
from UserAssignedToGroup
  GroupId = $eventContext.eventSourceId
  # or
  GroupId = $eventSourceId
```

## Event Source ID Shorthand

The `$eventSourceId` is a shorthand for `$eventContext.eventSourceId`:

```
from UserCreated key $eventSourceId
  Name = name
```

## Common Patterns

### Audit Fields

Track when things happen:

```
from RecordCreated
  Name = name
  CreatedAt = $eventContext.occurred

from RecordUpdated
  Name = name
  UpdatedAt = $eventContext.occurred
```

### Correlation Tracking

Link related operations:

```
from OrderPlaced
  OrderNumber = orderNumber
  CorrelationId = $eventContext.correlationId
  PlacedAt = $eventContext.occurred
```

### Sequence Tracking

Track event order:

```
from StateChanged
  CurrentState = state
  SequenceNumber = $eventContext.sequenceNumber
  ChangedAt = $eventContext.occurred
```

### Parent-Child Relationships

Use event source ID for relationships:

```
children members id userId
  from UserAddedToGroup key userId
    parent $eventContext.eventSourceId
    Role = role
    AddedAt = $eventContext.occurred
```

## In Composite Keys

Event context can be used in composite keys:

```
from EventProcessed
  key ProcessingKey {
    EventId = eventId
    SequenceNumber = $eventContext.sequenceNumber
    CorrelationId = $eventContext.correlationId
  }
  ProcessedAt = $eventContext.occurred
```

## In Templates

Event context works in string templates:

```
from OrderShipped
  TrackingInfo = `Shipped at ${$eventContext.occurred}`
  Reference = `${orderNumber}-${$eventContext.sequenceNumber}`
```

## Examples

### User Activity Log

```
projection UserActivity => UserActivityReadModel
  from UserLoggedIn
    LastLogin = $eventContext.occurred
    LastLoginSequence = $eventContext.sequenceNumber
    count LoginCount

  from UserAction
    LastAction = actionType
    LastActionTime = $eventContext.occurred
    LastCorrelation = $eventContext.correlationId
```

### Versioned Document

```
projection Document => DocumentReadModel
  from DocumentCreated
    Title = title
    Content = content
    Version = $eventContext.sequenceNumber
    CreatedAt = $eventContext.occurred

  from DocumentUpdated
    Content = content
    Version = $eventContext.sequenceNumber
    UpdatedAt = $eventContext.occurred
```

### Group Membership with Audit

```
projection Group => GroupReadModel
  from GroupCreated
    Name = name
    CreatedAt = $eventContext.occurred
    CreatedBy = $eventContext.eventSourceId

  children members id userId
    from UserAddedToGroup key userId
      parent groupId
      Name = userName
      Role = role
      AddedAt = $eventContext.occurred
      AddedSequence = $eventContext.sequenceNumber
```

### Order Processing

```
projection Order => OrderReadModel
  from OrderPlaced
    OrderNumber = orderNumber
    CustomerId = customerId
    Total = total
    PlacedAt = $eventContext.occurred
    TrackingId = $eventContext.correlationId

  from OrderShipped
    ShippedAt = $eventContext.occurred
    ShipmentSequence = $eventContext.sequenceNumber
    Status = "Shipped"
```

## Best Practices

1. **Audit Timestamps**: Use `occurred` for created/updated timestamps
2. **Correlation**: Use `correlationId` to link related operations
3. **Versioning**: Use `sequenceNumber` for version tracking
4. **Relationships**: Use `eventSourceId` for parent-child relationships
5. **Debugging**: Include correlation IDs and timestamps for troubleshooting
6. **Immutability**: Event context values are immutable and reliable
