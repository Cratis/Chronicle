# Expressions

Expressions define values in the Projection Declaration Language. They can reference event properties, event context, literals, and create formatted strings.

## Expression Types

### Property Paths

Access properties from the event using dot notation:

```pdl
name
email
address.city
contactInfo.email
order.customer.name
```

**Examples:**

```pdl
from UserRegistered
  Name = name
  Email = contactInfo.email
  City = address.city
```

### Event Context

Access event metadata using `$eventContext.{property}`:

```pdl
$eventContext.occurred
$eventContext.sequenceNumber
$eventContext.correlationId
$eventContext.eventSourceId
```

**Available Properties:**
- `occurred` - When the event occurred (timestamp)
- `sequenceNumber` - Sequence number in the event stream
- `correlationId` - Correlation ID for tracking related operations
- `eventSourceId` - The event source identifier

**Examples:**

```pdl
from OrderPlaced
  PlacedAt = $eventContext.occurred
  Sequence = $eventContext.sequenceNumber
  Correlation = $eventContext.correlationId
```

See [Event Context](event-context.md) for more details.

### Event Source ID Shorthand

Quick access to the event source identifier:

```pdl
$eventSourceId
```

This is equivalent to `$eventContext.eventSourceId`.

**Example:**

```pdl
from UserAssignedToGroup
  GroupId = $eventSourceId
  UserId = userId
```

### Identity (Caused By)

Access the identity that caused the event using `$causedBy`:

```pdl
$causedBy.subject
$causedBy.name
$causedBy.userName
```

**Available Properties:**
- `subject` - The identifier of the identity (unique ID)
- `name` - Display name of the identity
- `userName` - Username of the identity

**Examples:**

```pdl
from OrderPlaced
  CreatedBySubject = $causedBy.subject
  CreatedByName = $causedBy.name
  CreatedByUser = $causedBy.userName

from DocumentUpdated
  UpdatedBy = `${$causedBy.name} (${$causedBy.userName})`
  UpdatedById = $causedBy.subject
```

### Literals

Direct values of various types:

**Boolean:**

```pdl
IsActive = true
IsDeleted = false
```

**String:**

```pdl
Status = "Pending"
Category = "Electronics"
```

**Number:**

```pdl
Count = 0
Price = 99.99
Tax = 0.08
```

**Null:**

```pdl
OptionalField = null
```

**Examples:**

```pdl
from AccountCreated
  Balance = 0.0
  IsActive = true
  Status = "New"
  Notes = null
```

### String Templates

Create formatted strings with embedded expressions:

```pdl
`${expression}`
`${firstName} ${lastName}`
`Order: ${orderNumber}`
```

**Syntax:**
- Wrap in backticks: `` ` ``
- Use `${...}` for expression substitution
- Can include multiple expressions

**Examples:**

```pdl
from PersonRegistered
  FullName = `${firstName} ${lastName}`
  DisplayInfo = `${name} (${email})`

from OrderPlaced
  Reference = `ORD-${orderNumber}`
  Summary = `Order ${orderNumber} for ${customerName}`
```

## Expression Context

Expressions are evaluated in different contexts:

### Event Data Context

When mapping from events, expressions reference event properties:

```pdl
from UserCreated
  Name = name           # References event.name
  Email = email         # References event.email
```

### Event Context

Special `$eventContext` provides metadata:

```pdl
from UserCreated
  CreatedAt = $eventContext.occurred
  CreatedBy = $eventContext.eventSourceId
```

### Mixed Context

Combine event data with event context:

```pdl
from OrderPlaced
  OrderNumber = orderNumber                    # Event data
  PlacedAt = $eventContext.occurred           # Event context
  Reference = `${orderNumber}-${$eventContext.sequenceNumber}`  # Both
```

## Expression Compilation

The declaration syntax is transformed when compiled into Chronicle projection definitions:

| Declaration Syntax | Compiled Format | Description |
|-------------------|----------------|-------------|
| `name` | `name` | Event property remains unchanged |
| `address.city` | `address.city` | Nested properties remain unchanged |
| `$eventContext.occurred` | `$eventContext(occurred)` | Dot notation becomes function-style |
| `$eventSourceId` | `$eventSourceId` | Remains unchanged |
| `true` | `true` | Literals remain unchanged |
| `` `${name}` `` | `` `${name}` `` | Templates remain unchanged |

This transformation ensures compatibility with Chronicle's internal expression format while keeping the declaration syntax clean and intuitive.

## Type Compatibility

Expressions must be compatible with the target property type:

```csharp
public class UserReadModel
{
    public string Name { get; set; }        // Requires string
    public int LoginCount { get; set; }     // Requires number
    public bool IsActive { get; set; }      // Requires boolean
    public DateTime CreatedAt { get; set; } // Requires timestamp
}
```

**Valid:**

```pdl
Name = name                              # string
LoginCount = 0                          # number
IsActive = true                         # boolean
CreatedAt = $eventContext.occurred      # timestamp
```

**Invalid:**

```pdl
Name = 123                              # number to string (invalid)
LoginCount = "five"                     # string to number (invalid)
IsActive = "yes"                        # string to boolean (invalid)
```

## Nested Property Access

Access deeply nested properties:

```pdl
from OrderPlaced
  CustomerEmail = order.customer.contactInfo.email
  ShippingCity = order.shipping.address.city
  BillingStreet = billing.address.line1
```

## Expressions in Different Contexts

### Property Assignments

```pdl
Property = expression
```

Examples:

```pdl
Name = name
Total = order.total
CreatedAt = $eventContext.occurred
DisplayName = `${firstName} ${lastName}`
```

### Keys

```pdl
key expression
```

Examples:

```pdl
from UserCreated key userId
from OrderPlaced key $eventSourceId
from LineItem key order.id
```

### Composite Keys

```pdl
key Type {
  Property = expression
  ...
}
```

Examples:

```pdl
key OrderLineKey {
  OrderId = orderId
  LineNumber = lineNumber
  Sequence = $eventContext.sequenceNumber
}
```

### Arithmetic Operations

```pdl
add Property by expression
subtract Property by expression
```

Examples:

```pdl
add Balance by amount
subtract Stock by quantity
add Total by lineItem.total
```

### Parent Keys

```pdl
parent expression
```

Examples:

```pdl
parent groupId
parent $eventContext.eventSourceId
parent order.customerId
```

### Child Identifiers

```pdl
id expression
```

Examples:

```pdl
children members identified by userId
children items identified by lineNumber
children versions identified by $eventContext.sequenceNumber
```

## Best Practices

1. **Use Property Paths**: Directly access nested properties rather than mapping intermediate objects
2. **Event Context for Metadata**: Use `$eventContext` for timestamps, IDs, and correlation
3. **Templates for Display**: Create formatted display values with templates
4. **Literals for Defaults**: Set initial values with appropriate literal types
5. **Type Awareness**: Ensure expressions produce values compatible with target properties
6. **Shorthand When Clear**: Use `$eventSourceId` shorthand for clarity
7. **Null Handling**: Use `null` literal for optional fields

## Common Patterns

### Audit Fields

```pdl
CreatedAt = $eventContext.occurred
CreatedBy = $eventContext.eventSourceId
LastUpdated = $eventContext.occurred
Version = $eventContext.sequenceNumber
```

### Display Values

```pdl
FullName = `${firstName} ${lastName}`
Address = `${street}, ${city}, ${state} ${zipCode}`
Summary = `${product} x ${quantity} @ ${price}`
```

### Nested Access

```pdl
Email = contact.email
Phone = contact.phoneNumbers.primary
City = addresses.shipping.city
```

### Default Values

```pdl
Status = "Pending"
IsActive = true
Count = 0
Balance = 0.0
Notes = null
```

## See Also

- [Event Context](event-context.md) - Detailed event context properties
- [Property Mapping](property-mapping.md) - Using expressions in mappings
- [Keys](keys.md) - Expressions in key definitions
