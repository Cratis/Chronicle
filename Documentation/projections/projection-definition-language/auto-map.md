# Auto-Map

AutoMap automatically copies properties with matching names from the event to the read model, reducing boilerplate when property names align.

## Basic Syntax

```
automap
```

## Projection-Level AutoMap

Apply AutoMap to all events in the projection:

```
projection User => UserReadModel
  automap
```

This automatically maps all matching properties from every event to the read model.

## Event-Level AutoMap

Apply AutoMap to a specific event:

```
projection User => UserReadModel
  from UserRegistered automap
    IsActive = true
```

This auto-maps matching properties from `UserRegistered`, then applies the explicit `IsActive` mapping.

## Join-Level AutoMap

Apply AutoMap within join blocks:

```
join Group on GroupId
  events GroupCreated, GroupRenamed
  automap
```

## Children-Level AutoMap

Apply AutoMap to children collections:

```
children members id userId
  automap

  from UserAddedToGroup
    Role = role
```

## Combining with Explicit Mappings

AutoMap runs first, then explicit mappings override or add properties:

```
from UserRegistered automap
  IsActive = true
  CreatedAt = $eventContext.occurred
```

If the event has `Name` and `Email` properties, they're auto-mapped. Then `IsActive` and `CreatedAt` are set explicitly.

## Matching Rules

AutoMap matches properties when:
- Property names are identical (case-sensitive)
- Both source (event) and target (read model) have the property
- Types are compatible

Example event:
```csharp
public record UserRegistered(string Name, string Email, int Age);
```

Example read model:
```csharp
public class UserReadModel
{
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
}
```

With `automap`, `Name`, `Email`, and `Age` are automatically copied. `IsActive` must be set explicitly.

## Multiple Levels

AutoMap can be used at multiple levels:

```
projection Group => GroupReadModel
  automap                      # Projection level

  from GroupCreated
    Description = description

  join Team on TeamId
    events TeamCreated
    automap                    # Join level

  children members id userId
    automap                    # Children level

    from UserAdded
      Role = role
```

## Scope and Precedence

1. **Projection-level** AutoMap applies to all `from` events
2. **Event-level** AutoMap applies only to that specific event
3. **Explicit mappings** always override AutoMap values
4. **Join-level** AutoMap applies to joined events
5. **Children-level** AutoMap applies to child events

## Examples

### Simple AutoMap

```
projection Product => ProductReadModel
  from ProductCreated automap
```

If `ProductCreated` has properties `Name`, `Price`, `Description` that match the read model, they're automatically mapped.

### AutoMap with Overrides

```
projection Order => OrderReadModel
  from OrderPlaced automap
    Status = "Pending"
    PlacedAt = $eventContext.occurred
```

Matching properties are auto-mapped, then `Status` and `PlacedAt` are set explicitly.

### Projection-Level with Event-Specific Mappings

```
projection User => UserReadModel
  automap

  from UserRegistered
    IsActive = true

  from UserDeactivated
    IsActive = false
    DeactivatedAt = $eventContext.occurred
```

All matching properties from both events are auto-mapped, plus explicit mappings.

### Join with AutoMap

```
projection Order => OrderReadModel
  from OrderPlaced automap

  join Customer on CustomerId
    events CustomerCreated, CustomerUpdated
    automap
```

### Children with Selective Overrides

```
projection Group => GroupReadModel
  from GroupCreated automap

  children members id userId
    automap

    from UserAddedToGroup
      Status = "Active"
```

## When to Use AutoMap

**Use AutoMap when:**
- Event and read model property names align
- You want to reduce boilerplate
- Properties have compatible types
- You're following naming conventions

**Avoid AutoMap when:**
- Property names differ significantly
- You need transformations
- Only a few properties match
- Explicit mappings aid clarity

## Best Practices

1. **Combine with Explicit Mappings**: Use AutoMap for bulk copying, explicit mappings for specifics
2. **Document Assumptions**: AutoMap relies on naming conventions being consistent
3. **Event-Level for Precision**: Apply AutoMap per event if only some events have matching properties
4. **Projection-Level for Consistency**: Use projection-level AutoMap when all events follow the same schema
