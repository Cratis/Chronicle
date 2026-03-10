# Projection Declaration Language

The Projection Declaration Language (PDL) is a modern, indentation-based language for defining Chronicle projections without writing code. It provides an elegant, compact syntax that reads as rules applied to events. Declarations written in PDL compile into projection definitions with zero loss of functionality.

## Overview

The PDL allows you to:

- Define projections declaratively using a simple, readable syntax
- Test and iterate on projections quickly in the Workbench
- Create projections without recompiling your application
- Visualize projection results immediately
- Preview results ad-hoc without a read model target
- Support all Chronicle projection capabilities

## Mental Model

> "When event **X** happens, apply these effects."

- `projection` defines the read model target
- `from Event` defines a rule for an event
- Assignments and operations are **effects** applied to the read model
- `children` and `join` create scoped mutation contexts
- Defaults remove noise; overrides are explicit

## Basic Structure

Every projection definition starts with a projection declaration and contains one or more directives or blocks.

### With an explicit read model target

```pdl
projection {Name} => {ReadModelType}
  {directives and blocks}
```

Example:

```pdl
projection User => UserReadModel
  from UserRegistered
    Name = name
    Email = email
    IsActive = true
```

### Without an explicit read model (inferred)

The `=> ReadModelType` part is optional. When omitted, the read model schema is **inferred** from the events used in the projection — Chronicle automatically derives the target type from the event properties.

```pdl
projection {Name}
  {directives and blocks}
```

Example:

```pdl
projection User
  from UserRegistered
```

> **When to use inferred projections**
>
> Inferred projections are designed for **ad-hoc exploration and preview only**.
> They enable you to query the event log without first defining a read model type.
> An inferred projection can never be registered as a permanent projection — you must always
> specify an explicit `=> ReadModelType` when saving a projection.

#### Type compatibility

When multiple `from` blocks contribute the same property to the inferred schema, their property types must be compatible. If two events define a property with the same name but different incompatible types, the compiler reports an error:

```pdl
projection BadProjection
  from EventA   // EventA.value is string
  from EventB   // EventB.value is int  → error: incompatible types
```

## Key Features

- **Indentation-based**: Structure defined by indentation (spaces only, no tabs)
- **Event-driven**: Rules trigger when events occur
- **AutoMap**: Automatically map matching property names
- **Inferred schema**: Read model can be omitted for ad-hoc preview queries
- **Expressions**: Support for property paths, event context, literals, and templates
- **Operations**: Counters, arithmetic, assignments
- **Relationships**: Joins and nested children
- **Removal**: Remove instances based on events or joined events
- **Composite Keys**: Multi-property keys for complex scenarios

## Topics

- [From Event](from-event.md) - Define rules that trigger when events occur
- [Property Mapping](property-mapping.md) - Map event data to read model properties
- [Auto-Map](auto-map.md) - Automatically map matching properties
- [Keys](keys.md) - Explicit and composite keys for projection instances
- [Event Context](event-context.md) - Access event metadata like timestamps and correlation IDs
- [From Every](from-every.md) - Apply rules to all events
- [Counters](counters.md) - Increment, decrement, and count operations
- [Arithmetic](arithmetic.md) - Add and subtract operations
- [Joins](joins.md) - Combine data from related events
- [Children](children.md) - Define nested collections
- [Removal](removal.md) - Remove projection instances based on events
- [Expressions](expressions.md) - Understanding expression syntax
- [Grammar (EBNF)](grammar.md) - Complete formal grammar specification

## Example Projection

Here's a comprehensive example demonstrating multiple features:

```pdl
projection Group => GroupReadModel
  from GroupCreated
    Name = name
    Description = description
    CreatedAt = $eventContext.occurred

  from GroupRenamed
    Name = name
    UpdatedAt = $eventContext.occurred

  children members identified by userId
    from UserAddedToGroup key userId
      parent groupId
      Name = userName
      Role = role
      AddedAt = $eventContext.occurred

    from UserRoleChanged key userId
      parent groupId
      Role = role

    remove with UserRemovedFromGroup key userId
      parent groupId

  remove with GroupDeleted
```

This projection:
- Creates a group from `GroupCreated` events
- Updates the name when `GroupRenamed` occurs
- Manages a collection of members
- Tracks when users are added and their roles changed
- Removes members when they leave
- Removes the entire group when deleted

## Getting Started

Start with simple projections and gradually add complexity as needed. The Projection Declaration Language is designed to be intuitive and readable, making it easy to understand what a projection does at a glance.
