# From Event

The `from` directive defines a rule that triggers when a specific event occurs. This is the primary way to populate and update read models from events.

## Basic Syntax

```pdl
from {EventType}
  {mappings and operations}
```pdl

## Simple Example

```pdl
projection User => UserReadModel
  from UserRegistered
    Name = name
    Email = email
    IsActive = true
```pdl

When a `UserRegistered` event occurs, it creates or updates a `UserReadModel` instance with the specified properties.

## Multiple Events (Compact Syntax)

You can define multiple events on the same line separated by commas when they share the same mappings and configuration:

```pdl
projection TransportRoute => TransportRouteReadModel
  automap
  from HubRouteAdded key id, WarehouseRouteAdded key id
```pdl

This is equivalent to:

```pdl
projection TransportRoute => TransportRouteReadModel
  automap
  from HubRouteAdded key id
  from WarehouseRouteAdded key id
```pdl

Each event can have its own inline key specification:

```pdl
from EventA key idA, EventB key idB, EventC
  automap
  Property = value
```pdl

## With Keys

Specify which property identifies the projection instance:

```pdl
from UserAssignedToGroup key userId
  GroupId = $eventContext.eventSourceId
  AssignedAt = $eventContext.occurred
```pdl

See [Keys](keys.md) for more details on key handling.

## With AutoMap

Automatically map matching properties:

```pdl
from UserRegistered automap
  IsActive = true
```pdl

AutoMap copies properties with matching names from the event to the read model, then applies any explicit mappings.

See [Auto-Map](auto-map.md) for more details.

## Multiple From Blocks

A projection can have multiple `from` blocks for different events:

```pdl
projection User => UserReadModel
  from UserRegistered
    Name = name
    Email = email
    CreatedAt = $eventContext.occurred

  from UserEmailChanged
    Email = email
    UpdatedAt = $eventContext.occurred

  from UserDeactivated
    IsActive = false
    DeactivatedAt = $eventContext.occurred
```pdl

## With Parent Key

When used within [Children](children.md) blocks, you can specify the parent relationship:

```pdl
children members id userId
  from UserAddedToGroup key userId
    parent groupId
    Role = role
```pdl

## Operations

Within a `from` block, you can use:

- **Property assignments**: `Name = value`
- **Counters**: `increment`, `decrement`, `count`
- **Arithmetic**: `add`, `subtract`
- **AutoMap**: Automatically map matching properties
- **Keys**: Specify instance identification
- **Composite Keys**: Multi-property keys

## Event Context

Access event metadata using `$eventContext`:

```pdl
from UserLoggedIn
  LastLogin = $eventContext.occurred
  LastSequenceNumber = $eventContext.sequenceNumber
  count LoginCount
```pdl

See [Event Context](event-context.md) for available properties.

## Nested Indentation

All mappings and operations within a `from` block must be indented:

```pdl
from UserCreated
  Name = name          # Indented
  Email = email        # Indented
  IsActive = true      # Indented
```pdl

## Examples

### Simple Property Mapping

```pdl
from OrderPlaced
  CustomerId = customerId
  Total = total
  Status = "Pending"
```pdl

### Using Templates

```pdl
from PersonRegistered
  FullName = `${firstName} ${lastName}`
  Email = email
```pdl

### Counter Operations

```pdl
from PageViewed
  count ViewCount
  LastViewedAt = $eventContext.occurred
```pdl

### Arithmetic Operations

```pdl
from PaymentReceived
  add Balance by amount
  LastPaymentDate = $eventContext.occurred
```pdl

### Complex Example

```pdl
from OrderPlaced key orderId
  CustomerId = customerId
  OrderNumber = orderNumber
  Total = total
  Status = "Pending"
  PlacedAt = $eventContext.occurred
  increment TotalOrders
```pdl
