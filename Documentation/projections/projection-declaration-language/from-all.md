# From All

The `from all` directive applies mappings to every event that affects the projection, regardless of event type. Unlike `from Event`, which targets a specific event type, `from all` runs for every event processed by the projection. This is useful for setting common properties like last-update timestamps without repeating the mapping in every `from` block.

## Basic Syntax

```pdl
from all
  {mappings}
```

## Simple Example

```pdl
projection User => UserReadModel
  from all
    LastUpdated = $eventContext.occurred

  from UserRegistered
    Name = name
    Email = email

  from UserEmailChanged
    Email = email
```

Both `UserRegistered` and `UserEmailChanged` events set `LastUpdated`, while each `from` block handles its own properties.

## With AutoMap

Apply AutoMap across all events:

```pdl
projection Product => ProductReadModel
  from all
    automap
    LastModified = $eventContext.occurred
```

## Common Patterns

### Audit Fields

Track when the projection was last modified:

```pdl
from all
  LastModified = $eventContext.occurred
  LastSequenceNumber = $eventContext.sequenceNumber
  LastCorrelationId = $eventContext.correlationId
```

### Event Source Tracking

Store which event source last modified the projection:

```pdl
from all
  LastModifiedBy = $eventContext.eventSourceId
  LastModifiedAt = $eventContext.occurred
```

### Version Tracking

Maintain the latest event sequence for optimistic concurrency:

```pdl
from all
  Version = $eventContext.sequenceNumber
  UpdatedAt = $eventContext.occurred
```

## Examples

### User Profile with Audit

```pdl
projection User => UserReadModel
  from all
    LastUpdated = $eventContext.occurred
    LastSequenceNumber = $eventContext.sequenceNumber

  from UserRegistered
    Name = name
    Email = email
    CreatedAt = $eventContext.occurred

  from UserEmailChanged
    Email = email

  from UserNameChanged
    Name = name
```

### Order with Tracking

```pdl
projection Order => OrderReadModel
  from all
    LastEventTime = $eventContext.occurred
    EventCount = $eventContext.sequenceNumber

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
```

## Difference Between `from all` and `every`

| Feature | `from all` | `every` |
|---------|-------------|---------|
| Applies to | All event types | All event types |
| Scope | Whole projection | Whole projection |
| AutoMap support | Yes | Yes |
| Typical use | "subscribe to every event" | "common fields for every event" |

Both directives process all events. The distinction is conceptual: `from all` expresses that the projection *subscribes* to all event types, while `every` adds common per-event side effects. In practice, both achieve the same result.

## When to Use

**Use `from all` when:**

- Setting last-modified timestamps
- Tracking sequence numbers or correlation IDs
- Maintaining version counters
- Applying AutoMap universally across all event types

**Avoid `from all` when:**

- Only specific events should update a property
- Different event types require different mapping logic
