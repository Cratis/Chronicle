# Auto-Map

AutoMap automatically copies properties with matching names from the event to the read model, reducing boilerplate when property names align.

**AutoMap is enabled by default for all projections.** To disable it, use the `no automap` directive.

## Default Behavior

By default, all property mappings automatically copy matching properties:

```pdl
projection User => UserReadModel
  from UserRegistered
    IsActive = true
```

This auto-maps all matching properties from `UserRegistered`, then applies the explicit `IsActive` mapping.

## Disabling AutoMap

Use `no automap` to disable automatic property mapping when needed.

### Projection-Level Disable

Disable AutoMap for the entire projection:

```pdl
projection User => UserReadModel
  no automap

  from UserRegistered
    Name = name
    Email = email
```

All properties must be mapped explicitly.

### Event-Level Disable

Disable AutoMap for a specific event:

```pdl
projection User => UserReadModel
  from UserRegistered
    no automap
    Name = name
    Email = email

  from UserUpdated
    # AutoMap still enabled here
    UpdatedAt = $eventContext.occurred
```

### Join-Level Disable

Disable AutoMap within join blocks:

```pdl
join Group on GroupId
  events GroupCreated, GroupRenamed
  no automap
  Name = name
```

### Children-Level Disable

Disable AutoMap for children collections:

```pdl
children members id userId
  no automap

  from UserAddedToGroup
    UserId = userId
    Role = role
```

## Combining with Explicit Mappings

AutoMap runs first, then explicit mappings override or add properties:

```pdl
from UserRegistered
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

With default AutoMap, `Name`, `Email`, and `Age` are automatically copied. `IsActive` must be set explicitly.

## Scope and Precedence

1. **Projection-level** `no automap` disables AutoMap for all events
2. **Event-level** `no automap` applies only to that specific event
3. **Explicit mappings** always override AutoMap values when both exist
4. **Join-level** `no automap` applies to joined events
5. **Children-level** `no automap` applies to child events
6. **Default is Enabled** - AutoMap works unless explicitly disabled

## Examples

### Default AutoMap

```pdl
projection Product => ProductReadModel
  from ProductCreated
    # Name, Price, Description auto-mapped if they exist
    CreatedAt = $eventContext.occurred
```

### Disable AutoMap Entirely

```pdl
projection Order => OrderReadModel
  no automap

  from OrderPlaced
    OrderId = orderId
    CustomerId = customerId
    Total = total
    Status = "Pending"
```

### Mixed Approach

```pdl
projection User => UserReadModel
  from UserRegistered
    # AutoMap enabled (default)
    IsActive = true

  from UserProfileUpdated
    no automap
    # Must map everything explicitly
    Name = fullName
    Email = emailAddress
```

### Projection-Level Disable with Event-Level Override

```pdl
projection User => UserReadModel
  no automap

  from UserRegistered
    Name = name
    Email = email

  from UserDeactivated
    # Still disabled - no automap is inherited
    IsActive = false
    DeactivatedAt = $eventContext.occurred
```

### Join with Disabled AutoMap

```pdl
projection Order => OrderReadModel
  from OrderPlaced
    # AutoMap enabled

  join Customer on CustomerId
    events CustomerCreated, CustomerUpdated
    no automap
    CustomerName = name
```

### Children with Selective Disable

```pdl
projection Group => GroupReadModel
  from GroupCreated
    # AutoMap enabled

  children members id userId
    no automap

    from UserAddedToGroup
      UserId = userId
      Status = "Active"
```

## When to Disable AutoMap

**Disable AutoMap when:**
- Event and read model property names don't align
- You need explicit control over all mappings
- Property transformations are required
- You want to be explicit for code clarity
- Only a few properties actually match

**Keep AutoMap (default) when:**
- Event and read model property names align
- You want to reduce boilerplate
- Properties have compatible types
- You're following naming conventions
- Most properties map directly

## Best Practices

1. **Leverage the Default**: AutoMap reduces boilerplate for most cases
2. **Disable Selectively**: Use `no automap` only when needed
3. **Document Assumptions**: AutoMap relies on naming conventions being consistent
4. **Event-Level for Precision**: Apply `no automap` per event if only some events need explicit control
5. **Explicit Overrides**: Let AutoMap handle the bulk, use explicit mappings for specifics

## Migration from Explicit AutoMap

If you're updating existing projections that used `automap` directive:
- Remove all `automap` directives - they're now redundant (default behavior)
- Add `no automap` only where you previously didn't have `automap`
- Existing explicit mappings continue to work and override auto-mapped values
