# Keys

Keys identify individual projection instances. They determine which read model instance to create or update when an event occurs.

## Explicit Keys

Specify which property from the event identifies the instance:

```
from UserRegistered key userId
  Name = name
  Email = email
```

The `userId` from the event becomes the key for the `UserReadModel` instance.

## Inline Key Syntax

Keys can be specified inline on the `from` statement:

```
from OrderPlaced key orderId
  Total = total
  Status = "Pending"
```

## Multiple Events with Keys

When using multiple events in one `from` statement, each can have its own key:

```
from EventA key idA, EventB key idB, EventC
  automap
```

## Block Key Syntax

For more complex scenarios, use block syntax:

```
from OrderPlaced
  key orderId
  Total = total
```

## Composite Keys

Use composite keys when multiple properties together identify an instance:

```
from OrderCreated
  key OrderKey {
    CustomerId = customerId
    OrderNumber = orderNumber
  }
  Total = total
```

### Composite Key Structure

```
key {TypeName} {
  {Property} = {expression}
  {Property} = {expression}
  ...
}
```

The `{TypeName}` is the name of the composite key type.

### Composite Key with Event Context

You can include event context values in composite keys:

```
from LineItemAdded
  key LineItemKey {
    OrderId = orderId
    LineNumber = lineNumber
    SequenceNumber = $eventContext.sequenceNumber
  }
  Product = productName
  Quantity = quantity
```

## Default Key Behavior

If no key is specified, the event source ID is used as the key:

```
from UserRegistered
  Name = name
```

Equivalent to:

```
from UserRegistered key $eventSourceId
  Name = name
```

## Key in Children

Children must always specify an identifier:

```
children members id userId
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

```
from ProductCreated key productId
  Name = name
  Price = price
```

### Event Source as Key

```
from AccountCreated key $eventSourceId
  AccountNumber = accountNumber
  Balance = 0.0
```

### Nested Property as Key

```
from OrderPlaced key order.id
  Total = order.total
  CustomerId = customerId
```

### Composite Key Example

```
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

```
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

```
children orderLines id lineNumber
  from LineItemAdded key lineNumber
    parent orderId
    Product = productName
    Quantity = quantity
    Price = price
```

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

## Best Practices

1. **Be Explicit**: Specify keys when not using event source ID
2. **Match Domain**: Use business-meaningful identifiers
3. **Composite for Complexity**: Use composite keys for multi-property identifiers
4. **Consistent Naming**: Use consistent key property names across events
5. **Document Composites**: Composite keys benefit from clear type names
