# Convention-Based Mapping

The `FromEvent` attribute enables convention-based mapping where properties are automatically matched by name between events and read models, reducing the need for explicit property specifications.

## Basic Convention Mapping

Apply `FromEvent` at the class level to enable automatic property matching:

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[FromEvent<UserRegistered>]
public record User(
    [Key, FromEventSourceId]
    Guid Id,
    
    string Name,        // Automatically mapped from UserRegistered.Name
    string Email,       // Automatically mapped from UserRegistered.Email
    DateTimeOffset RegisteredAt);  // Automatically mapped from UserRegistered.RegisteredAt
```

This is equivalent to:

```csharp
public record User(
    [Key, FromEventSourceId]
    Guid Id,
    
    [SetFrom<UserRegistered>(nameof(UserRegistered.Name))]
    string Name,
    
    [SetFrom<UserRegistered>(nameof(UserRegistered.Email))]
    string Email,
    
    [SetFrom<UserRegistered>(nameof(UserRegistered.RegisteredAt))]
    DateTimeOffset RegisteredAt);
```

## Multiple Events

You can use multiple `FromEvent` attributes for different events:

```csharp
[FromEvent<UserRegistered>]
[FromEvent<UserProfileUpdated>]
public record UserProfile(
    [Key, FromEventSourceId]
    Guid Id,
    
    string Name,     // From UserRegistered and UserProfileUpdated
    string Email,    // From UserRegistered and UserProfileUpdated
    string Phone);   // From UserProfileUpdated only
```

Properties are matched by name. If an event doesn't have a matching property, that property is skipped for that event.

## Mixing Convention and Explicit Mapping

You can combine `FromEvent` with explicit property attributes:

```csharp
[FromEvent<AccountOpened>]
public record Account(
    [Key, FromEventSourceId]
    Guid Id,
    
    string Name,  // Convention-based from AccountOpened.Name
    
    [SetFrom<AccountOpened>(nameof(AccountOpened.InitialBalance))]
    [AddFrom<DepositMade>(nameof(DepositMade.Amount))]
    [SubtractFrom<WithdrawalMade>(nameof(WithdrawalMade.Amount))]
    decimal Balance);  // Explicit mapping for complex operations
```

In this example:
- `Name` is mapped automatically by convention
- `Balance` uses explicit attributes for multiple operations

## Complete Example

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

// Events with matching property names
[EventType]
public record EmployeeHired(
    string FirstName,
    string LastName,
    string Email,
    string Department,
    DateTimeOffset HiredAt);

[EventType]
public record EmployeeDepartmentChanged(
    string Department,
    DateTimeOffset ChangedAt);

[EventType]
public record SalaryAdjusted(decimal NewSalary);

// Read Model using convention-based mapping
[FromEvent<EmployeeHired>]
[FromEvent<EmployeeDepartmentChanged>]
public record Employee(
    [Key, FromEventSourceId]
    Guid Id,
    
    // These properties are automatically mapped by name
    string FirstName,
    string LastName,
    string Email,
    string Department,
    
    // This needs explicit mapping because it comes from a different property name
    [SetFrom<SalaryAdjusted>(nameof(SalaryAdjusted.NewSalary))]
    decimal Salary,
    
    // Track department changes
    [Count<EmployeeDepartmentChanged>]
    int DepartmentChangeCount);
```

## When to Use Convention-Based Mapping

**Use FromEvent when:**
- Property names match between events and read models
- You're mapping multiple properties from the same event
- You want to reduce boilerplate code
- The mapping is straightforward (simple Set operations)

**Use explicit attributes when:**
- Property names don't match
- You need complex operations (Add, Subtract, Increment, etc.)
- You're mapping from multiple events with different operations
- You want explicit control over each property mapping

## Property Matching Rules

1. **Name must match exactly** - Property names are case-sensitive
2. **Type compatibility** - Properties must be compatible types (automatic conversions may apply)
3. **Event must have property** - If the event doesn't have a matching property, it's skipped (no error)
4. **Read model must be writable** - Properties must be settable (which they are in records)

## Benefits

Convention-based mapping with `FromEvent`:
- **Reduces verbosity** - Less code to write and maintain
- **Improves readability** - Clear intent when most properties map directly
- **Easier refactoring** - Rename properties in both places without updating attributes
- **Best of both worlds** - Combine with explicit attributes where needed

## Best Practices

1. **Use consistent naming** between events and read models for convention mapping to work
2. **Document non-obvious mappings** with comments when mixing conventions and explicit attributes
3. **Start with FromEvent** and add explicit attributes only where needed
4. **Consider maintainability** - Sometimes explicit attributes are clearer even if more verbose
