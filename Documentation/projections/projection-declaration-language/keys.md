# Keys

Keys identify individual projection instances. They determine which read model instance to create or update when an event occurs.

## Explicit Keys

Specify which property from the event identifies the instance:

```pdl
from UserRegistered key userId
  Name = name
  Email = email
```

The `userId` from the event becomes the key for the `UserReadModel` instance.

## Inline Key Syntax

Keys can be specified inline on the `from` statement:

```pdl
from OrderPlaced key orderId
  Total = total
  Status = "Pending"
```

## Multiple Events with Keys

When using multiple events in one `from` statement, each can have its own key:

```pdl
from EventA key idA, EventB key idB, EventC
  automap
```

## Block Key Syntax

For more complex scenarios, use block syntax:

```pdl
from OrderPlaced
  key orderId
  Total = total
```

## Composite Keys

Use composite keys when multiple properties together identify an instance:

```pdl
from OrderCreated
  key OrderKey
    CustomerId = customerId
    OrderNumber = orderNumber
  Total = total
```

### Composite Key Structure

```pdl
key {TypeName}
  {Property} = {expression}
  {Property} = {expression}
  ...
```

The `{TypeName}` must match a complex type defined in your read model schema.

### Composite Key with Event Context and Causation

You can include event context and causation values in composite keys:

```pdl
from LineItemAdded
  key LineItemKey
    OrderId = orderId
    LineNumber = lineNumber
    SequenceNumber = $eventContext.sequenceNumber
    CreatedBy = $causedBy.subject
  Product = productName
  Quantity = quantity
```

## Default Key Behavior

If no key is specified, the event source ID is used as the key:

```pdl
from UserRegistered
  Name = name
```

Equivalent to:

```pdl
from UserRegistered key $eventSourceId
  Name = name
```

## Key in Children

Children must always specify an identifier:

```pdl
children members identified by userId
  from UserAddedToGroup key userId
    parent groupId
    Role = role
```

The `id userId` specifies the child identifier property, while `key userId` specifies which event property to use.

## Key Expressions

Keys can be:
- **Property paths**: `userId`, `order.id`, `data.customerId`
- **Event source ID**: `$eventSourceId`
- **Event context**: `$eventContext.correlationId`

## Examples

### Simple Key

```pdl
from ProductCreated key productId
  Name = name
  Price = price
```

### Event Source as Key

```pdl
from AccountCreated key $eventSourceId
  AccountNumber = accountNumber
  Balance = 0.0
```

### Nested Property as Key

```pdl
from OrderPlaced key order.id
  Total = order.total
  CustomerId = customerId
```

### Composite Key Example

```pdl
projection OrderLine => OrderLineReadModel
  from LineItemAdded
    key OrderLineKey {
      OrderId = orderId
      ProductId = productId
    }
    Quantity = quantity
    UnitPrice = unitPrice
    LineTotal = total
```

### Composite Key with Multiple Properties

```pdl
from ReservationMade
  key ReservationKey {
    HotelId = hotelId
    RoomNumber = roomNumber
    CheckInDate = checkInDate
  }
  GuestName = guestName
  Nights = nights
```

### Children with Keys

```pdl
children orderLines identified by lineNumber
  from LineItemAdded key lineNumber
    parent orderId
    Product = productName
    Quantity = quantity
    Price = price
```

## Literal (Constant) Keys

Use `literal "value"` to fix the key to a constant string. All events matching the `from` block update the **same** read model instance regardless of event source:

```pdl
from OrderPlaced
  key literal "global"
  count TotalOrders
```

Every `OrderPlaced` event, from every event source, increments `TotalOrders` on the single document with key `"global"`.

### Inline literal syntax

Literal keys also work inline on the `from` statement:

```pdl
from UserLoggedIn key literal "site-stats"
  count TotalLogins
```

### Combining literal keys with functions

Literal keys are especially useful with counter and arithmetic operations for global aggregates:

```pdl
projection SiteMetrics => SiteMetricsReadModel
  from UserRegistered
    key literal "metrics"
    count TotalUsers

  from UserLoggedIn
    key literal "metrics"
    increment ActiveSessions

  from UserLoggedOut
    key literal "metrics"
    decrement ActiveSessions

  from OrderPlaced
    key literal "metrics"
    count TotalOrders
    add TotalRevenue by amount
```

All events from all users accumulate into the single `SiteMetricsReadModel` document with key `"metrics"`.

## When to Use Each Approach

**Simple Keys (inline):**
- Single property identifies the instance
- Property name is clear from context
- Most common scenario

**Block Keys:**
- When you want visual separation
- Complex event structures
- Personal preference for readability

**Composite Keys:**
- Multiple properties together form identity
- Natural keys from business domain
- Partitioning or sharding scenarios
- Complex relationships

**Event Source ID:**
- Each event stream represents one instance
- Aggregate-based event sourcing
- One-to-one event stream to read model

**Literal (Constant) Keys:**
- All events update the same instance
- Global counters and system-wide aggregates
- Singleton read models

## Best Practices

1. **Be Explicit**: Specify keys when not using event source ID
2. **Match Domain**: Use business-meaningful identifiers
3. **Composite for Complexity**: Use composite keys for multi-property identifiers
4. **Consistent Naming**: Use consistent key property names across events
5. **Document Composites**: Composite keys benefit from clear type names
6. **Literal for Globals**: Use literal keys for system-wide or global aggregates
