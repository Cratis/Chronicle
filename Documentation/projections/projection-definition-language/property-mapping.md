# Property Mapping

Property mapping assigns values from events to read model properties. Mappings use the simple assignment syntax: `Property = expression`.

## Basic Mapping

Map an event property to a read model property:

```
from UserRegistered
  Name = name
  Email = email
```

This maps the `name` and `email` properties from the `UserRegistered` event to the corresponding properties on the read model.

## Nested Properties

Access nested properties using dot notation:

```
from UserRegistered
  Email = contactInfo.email
  Phone = contactInfo.phone
  City = address.city
```

## Literal Values

Assign literal values:

```
from UserRegistered
  Name = name
  IsActive = true
  Status = "Pending"
  Priority = 5
  CreatedAt = null
```

Supported literal types:
- **Boolean**: `true`, `false`
- **String**: `"text"` (double quotes)
- **Number**: `42`, `3.14`
- **Null**: `null`

## String Templates

Create formatted strings using template literals:

```
from PersonRegistered
  FullName = `${firstName} ${lastName}`
  DisplayInfo = `${name} (${email})`
```

Template syntax:
- Wrap in backticks: `` `template` ``
- Use `${expression}` for substitutions
- Can combine multiple expressions

## Event Source ID

The special identifier `$eventSourceId` provides the event source ID:

```
from UserAssignedToGroup
  GroupId = $eventContext.eventSourceId
  UserId = $eventSourceId
```

## Event Context

Access event metadata:

```
from UserRegistered
  Name = name
  CreatedAt = $eventContext.occurred
  SequenceNumber = $eventContext.sequenceNumber
  CorrelationId = $eventContext.correlationId
```

See [Event Context](event-context.md) for all available properties.

## Multiple Mappings

Define multiple mappings in a single `from` block:

```
from OrderPlaced
  OrderNumber = orderNumber
  CustomerId = customerId
  Total = total
  Status = "New"
  PlacedAt = $eventContext.occurred
  TaxRate = 0.08
```

## Property Types

The read model property type determines what values are valid:

```
from ProductCreated
  Name = name              # string
  Price = price            # decimal/number
  IsAvailable = true       # boolean
  Stock = 0                # integer
  Category = null          # nullable
```

## Examples

### User Profile

```
from UserProfileUpdated
  FirstName = firstName
  LastName = lastName
  Email = email
  Bio = bio
  UpdatedAt = $eventContext.occurred
```

### Order with Calculated Fields

```
from OrderPlaced
  OrderId = orderId
  CustomerId = customerId
  Subtotal = subtotal
  Tax = tax
  Total = total
  Status = "Pending"
  Reference = `ORD-${orderNumber}`
  PlacedAt = $eventContext.occurred
```

### Nested Data

```
from CompanyRegistered
  CompanyName = name
  Email = contactInfo.email
  Phone = contactInfo.phone
  Street = address.street
  City = address.city
  State = address.state
  ZipCode = address.zipCode
```

### With Literals and Context

```
from AccountCreated
  AccountNumber = accountNumber
  Balance = 0.0
  IsActive = true
  AccountType = "Standard"
  OpenedAt = $eventContext.occurred
  OpenedBy = $eventContext.eventSourceId
```

## Best Practices

1. **Be Explicit**: Name properties clearly to show intent
2. **Use Defaults**: Set initial values with literals when appropriate
3. **Leverage Context**: Use event context for audit fields
4. **Templates for Display**: Use templates to create formatted display values
5. **Nested Access**: Directly access nested properties rather than mapping intermediate objects
