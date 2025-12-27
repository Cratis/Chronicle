# Projection DSL

The Projection DSL (Domain-Specific Language) provides a simple, text-based way to define projections without writing code. This is particularly useful in the Workbench where you can experiment with projections and see results in real-time.

## Overview

The DSL allows you to:

- Define projections declaratively using a simple syntax
- Test and iterate on projections quickly
- Create projections without recompiling your application
- Visualize projection results immediately

## Basic Syntax

A projection definition starts with the read model name, followed by pipe-separated (`|`) statements that define the projection's behavior.

```
{ReadModelName}
| {statement}
| {statement}
...
```

## Read Model Declaration

Every projection starts by declaring the target read model:

```
Users
```

## Property Mappings

Map properties from events to the read model using the equals operator (`=`).

### Simple Property Mapping

Map an event property directly to a read model property:

```
Users
| name=UserRegistered.name
| email=UserRegistered.email
```

This maps the `name` property from the `UserRegistered` event to the `name` property on the `Users` read model.

### Event Context Properties

Access event metadata using the `$eventContext` keyword:

```
Users
| occurred=$eventContext.occurred
| eventSourceId=$eventContext.eventSourceId
| causedBy=$eventContext.causedBy.name
| namespace=$eventContext.namespace
```

You can also map event context properties for specific events:

```
Users
| lastUpdated=UserUpdated.$eventContext.occurred
```

### Constant Values

Set a property to a constant value when a specific event occurs:

```
Users
| status="active" on UserRegistered
| status="inactive" on UserDeactivated
```

## Arithmetic Operations

### Add Operation

Add a value from an event to a property:

```
Users
| totalPurchases+OrderCompleted.amount
```

This adds the `amount` from `OrderCompleted` events to the `totalPurchases` property.

### Subtract Operation

Subtract a value from a property:

```
Users
| balance-PaymentMade.amount
```

### Increment Operation

Increment a counter when an event occurs:

```
Users
| orderCount increment by OrderStarted
```

### Decrement Operation

Decrement a counter when an event occurs:

```
Users
| orderCount decrement by OrderCancelled
```

### Count Operation

Count the number of times an event occurs:

```
Users
| completedOrders count OrderCompleted
```

## Keys

By default, the event source ID becomes the key for a read model instance. You can specify custom keys.

### Simple Key

Define which property from an event should be used as the key:

```
Users
| key=UserRegistered.userId
| name=UserRegistered.name
```

### Composite Keys

Create a composite key from multiple properties:

```
Users
| key=userId:UserRegistered.userId, tenantId:UserRegistered.tenantId
```

## Children (One-to-Many Relationships)

Define child collections using square brackets `[]`:

```
Users
| key=UserRegistered.userId
| name=UserRegistered.name
| orders=[
|    identified by orderId
|    key=OrderPlaced.orderId
|    total=OrderPlaced.total
|    status=OrderCompleted.status
| ]
```

The `identified by` clause specifies which property uniquely identifies each child item. Children support all the same operations as top-level projections, including nested children.

### Nested Children

Children can have their own children:

```
Users
| orders=[
|    identified by orderId
|    items=[
|       identified by itemId
|       name=ItemAdded.name
|       quantity=ItemAdded.quantity
|    ]
| ]
```

## Removed With

Specify which event should remove a read model instance:

```
Users
| name=UserRegistered.name
| removedWith UserDeleted
```

For children, the `removedWith` statement goes inside the children block:

```
Users
| orders=[
|    identified by orderId
|    total=OrderPlaced.total
|    removedWith OrderCancelled
| ]
```

## Complete Example

Here's a comprehensive example showing multiple features:

```
Users
| key=UserRegistered.userId
| name=UserRegistered.name
| email=UserRegistered.email
| registered=$eventContext.occurred
| totalSpent+OrderCompleted.total
| currentOrderCount increment by OrderStarted
| currentOrderCount decrement by OrderCompleted
| currentOrderCount decrement by OrderCancelled
| completedOrderCount count OrderCompleted
| status="active" on UserRegistered
| status="suspended" on UserSuspended
| orders=[
|    identified by orderId
|    key=OrderPlaced.orderId
|    total=OrderPlaced.total
|    placedAt=OrderPlaced.$eventContext.occurred
|    status="pending" on OrderPlaced
|    status="completed" on OrderCompleted
|    removedWith OrderCancelled
| ]
| removedWith UserDeleted
```

## Comments

Use `#` to add comments in your DSL:

```
Users
# This maps the user's name
| name=UserRegistered.name

# Track when the user registered
| registered=$eventContext.occurred
```

## Syntax Summary

| Pattern | Description | Example |
|---------|-------------|---------|
| `{property}={Event}.{eventProperty}` | Map event property to read model property | `name=UserRegistered.name` |
| `{property}+{Event}.{eventProperty}` | Add operation | `total+OrderCompleted.amount` |
| `{property}-{Event}.{eventProperty}` | Subtract operation | `balance-PaymentMade.amount` |
| `{property} increment by {Event}` | Increment counter | `count increment by ItemAdded` |
| `{property} decrement by {Event}` | Decrement counter | `count decrement by ItemRemoved` |
| `{property} count {Event}` | Count events | `orders count OrderPlaced` |
| `{property}={constant} on {Event}` | Set constant value | `status="active" on UserRegistered` |
| `{property}=$eventContext.{contextProperty}` | Map event context property | `occurred=$eventContext.occurred` |
| `key={Event}.{property}` | Simple key | `key=UserRegistered.userId` |
| `key={prop1}:{Event}.{eventProp1}, {prop2}:{Event}.{eventProp2}` | Composite key | `key=userId:UserRegistered.userId, tenantId:UserRegistered.tenantId` |
| `{property}=[...]` | Children definition | `orders=[...]` |
| `identified by {property}` | Identify children uniquely | `identified by orderId` |
| `removedWith {Event}` | Remove instance when event occurs | `removedWith UserDeleted` |

## Event Types

Event types are referenced by their friendly name (e.g., `UserRegistered`, `OrderPlaced`). The system resolves these to the actual event type identifiers internally.
