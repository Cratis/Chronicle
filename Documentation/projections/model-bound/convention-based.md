# Convention-Based Mapping with FromEvent

The `FromEvent` attribute enables convention-based automatic property mapping between events and read models, similar to the `.AutoMap()` functionality in regular projections. This attribute automatically maps properties with matching names, eliminating the need for explicit property-level attributes in many cases.

## How FromEvent Works

`FromEvent` performs automatic property mapping using these rules:

1. **Name matching**: Properties with identical names (case-sensitive) between event and read model are automatically mapped
2. **Type compatibility**: Property types must be compatible for assignment
3. **Recursive mapping**: Nested objects are automatically mapped recursively if their property names match
4. **Collection handling**: Arrays and collections are handled automatically
5. **Selective mapping**: Only properties that exist on both event and read model are mapped (missing properties are silently ignored)

This approach is equivalent to using `.AutoMap()` in regular projection classes but achieved through attributes.

## Basic Convention Mapping

Apply `FromEvent` at the class level to enable automatic property matching:

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[FromEvent<UserRegistered>]
public record User(
    [Key]
    Guid Id,

    string Name,        // Automatically mapped from UserRegistered.Name
    string Email,       // Automatically mapped from UserRegistered.Email
    DateTimeOffset RegisteredAt);  // Automatically mapped from UserRegistered.RegisteredAt
```

This is equivalent to:

```csharp
public record User(
    [Key]
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
    [Key]
    Guid Id,

    string Name,     // From UserRegistered and UserProfileUpdated
    string Email,    // From UserRegistered and UserProfileUpdated
    string Phone);   // From UserProfileUpdated only
```

Properties are matched by name. If an event doesn't have a matching property, that property is skipped for that event.

## Custom Key Specification

By default, `FromEvent` uses the event source identifier to identify which read model instance to update. You can specify a different property from the event to use as the key:

```csharp
[FromEvent<UserRegistered>(key: nameof(UserRegistered.UserId))]
public record User(
    [Key]
    Guid Id,

    string Name,
    string Email);
```

This is equivalent to using `.UsingKey()` in declarative projections:

```csharp
public class UserProjection : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .AutoMap()
        .From<UserRegistered>(_ => _
            .UsingKey(e => e.UserId));
}
```

### When to Use Custom Keys

Use the `key` parameter when:

1. **Event source ID doesn't match read model key**: The event's natural identifier differs from the event source ID
2. **Multiple instances per source**: A single event source creates multiple read model instances
3. **Cross-aggregate projections**: Events from one aggregate update read models keyed by a different aggregate

### Example with Custom Key

```csharp
[EventType]
public record OrderLineItemAdded(
    Guid OrderId,      // Key for the Order read model
    Guid LineItemId,   // Key for individual line items
    string ProductName,
    int Quantity,
    decimal Price);

// Order projection using OrderId as key
[FromEvent<OrderLineItemAdded>(key: nameof(OrderLineItemAdded.OrderId))]
public record Order(
    [Key]
    Guid Id,

    // Properties auto-mapped from OrderLineItemAdded
    decimal TotalAmount);
```

### Key Validation

The key property must exist on the event type. If you specify a non-existent property, you'll get a compile-time error:

```csharp
// ❌ This will throw InvalidPropertyForType exception
[FromEvent<UserRegistered>(key: "NonExistentProperty")]
public record User([Key] Guid Id, string Name);
```

## Mixing Convention and Explicit Mapping

You can combine `FromEvent` with explicit property attributes:

```csharp
[FromEvent<AccountOpened>]
public record Account(
    [Key]
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

## Relationship to Regular Projections AutoMap

`FromEvent` in model-bound projections provides the same automatic mapping functionality as `.AutoMap()` in regular projection classes. These approaches are equivalent:

**Model-bound projection with FromEvent:**

```csharp
[FromEvent<UserRegistered>]
[FromEvent<UserProfileUpdated>]
public record User(
    [Key] Guid Id,
    string Name,
    string Email,
    string Phone);
```

**Regular projection with AutoMap:**

```csharp
public class UserProjection : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .AutoMap()
        .From<UserRegistered>()
        .From<UserProfileUpdated>();
}
```

Both approaches automatically map properties with matching names and types.

## Nested Object Mapping

`FromEvent` automatically handles nested object mapping when property names and structures match:

```csharp
// Events with nested structures
[EventType]
public record CustomerRegistered(
    string FirstName,
    string LastName,
    string Email,
    Address BillingAddress,
    Address ShippingAddress);

[EventType]
public record CustomerAddressUpdated(
    Address BillingAddress,
    Address ShippingAddress);

public record Address(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country);

// Read model with automatic nested mapping
[FromEvent<CustomerRegistered>]
[FromEvent<CustomerAddressUpdated>]
public record Customer(
    [Key] Guid Id,

    // Simple properties mapped automatically
    string FirstName,
    string LastName,
    string Email,

    // Nested objects mapped recursively
    Address BillingAddress,
    Address ShippingAddress);
```

## Collection Mapping

Arrays and collections are automatically mapped when the element types and property names match:

```csharp
[EventType]
public record OrderCreated(
    string CustomerEmail,
    LineItem[] Items,
    string[] Tags);

public record LineItem(
    string ProductName,
    decimal UnitPrice,
    int Quantity);

[FromEvent<OrderCreated>]
public record Order(
    [Key] Guid Id,

    string CustomerEmail,  // Automatically mapped
    LineItem[] Items,      // Array mapped with nested objects
    string[] Tags);        // Simple array mapped
```

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
    [Key]
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

## Automatic Mapping Benefits

**FromEvent provides the same benefits as AutoMap:**

- **Reduces boilerplate**: Eliminates the need for individual `SetFrom` attributes on each property
- **Convention over configuration**: Property names drive the mapping automatically
- **Type safety**: Compile-time checking ensures compatible types
- **Nested support**: Automatically handles complex object hierarchies
- **Collection handling**: Arrays and lists are mapped recursively
- **Maintainable**: Renaming properties updates mappings automatically

## When to Use Convention-Based Mapping

**Use FromEvent when:**

- Property names match exactly between events and read models
- You're mapping multiple properties from the same event
- You want to reduce boilerplate code
- The mapping is straightforward (simple Set operations)
- You have consistent naming conventions across events and read models
- You need to map nested objects or collections with matching structures

**Use explicit attributes when:**

- Property names don't match between event and read model
- You need complex operations (Add, Subtract, Increment, Count, etc.)
- You're mapping from multiple events with different operations
- You want explicit control over each property mapping
- Properties require transformations or calculations
- You're mapping from different event property names

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

## Performance Considerations

Like `.AutoMap()` in regular projections, `FromEvent` performs property matching at projection definition time, not during event processing. There is no runtime performance penalty compared to explicit `SetFrom` attributes - both compile to the same internal representation.

## Advanced Scenarios

### Type Compatibility

`FromEvent` handles common type conversions automatically:

```csharp
[EventType]
public record ProductPriceChanged(double NewPrice);  // double in event

[FromEvent<ProductPriceChanged>]
public record Product(
    [Key] Guid Id,
    decimal NewPrice);  // decimal in read model - automatically converted
```

### Partial Property Mapping

Events don't need to have all read model properties. Missing properties are ignored:

```csharp
[EventType]
public record UserRegistered(string Email);  // Only has Email

[EventType]
public record UserProfileCompleted(string FirstName, string LastName, string Phone);

[FromEvent<UserRegistered>]      // Maps: Email
[FromEvent<UserProfileCompleted>] // Maps: FirstName, LastName, Phone
public record User(
    [Key] Guid Id,
    string Email,      // From UserRegistered
    string FirstName,  // From UserProfileCompleted
    string LastName,   // From UserProfileCompleted
    string Phone);     // From UserProfileCompleted
```

### Combining with Other Projection Features

`FromEvent` works seamlessly with other model-bound projection features:

```csharp
[FromEvent<AccountOpened>]           // Auto-maps: AccountNumber, CustomerName
[FromEvent<AccountDetailsUpdated>]   // Auto-maps: CustomerName, ContactEmail
public record Account(
    [Key] Guid Id,

    // Auto-mapped properties
    string AccountNumber,
    string CustomerName,
    string ContactEmail,

    // Explicit mappings for complex scenarios
    [SetFrom<AccountOpened>(nameof(AccountOpened.InitialDeposit))]
    [AddFrom<DepositMade>(nameof(DepositMade.Amount))]
    [SubtractFrom<WithdrawalMade>(nameof(WithdrawalMade.Amount))]
    decimal Balance,

    // Metadata tracking
    [FromEvery(contextProperty: "Occurred")]
    DateTimeOffset LastModified);
```

## Best Practices

1. **Use consistent naming conventions** between events and read models to maximize automatic mapping effectiveness
2. **Start with FromEvent** and add explicit attributes only where needed for complex scenarios
3. **Document mixed approaches** with comments when combining automatic and explicit mappings
4. **Consider maintainability** - Sometimes explicit attributes provide better clarity even if more verbose
5. **Leverage nested mapping** - Design nested types with consistent property names for recursive automatic mapping
6. **Group related events** - Use multiple `FromEvent` attributes for events that share similar property structures

## Summary

The `FromEvent` attribute provides powerful automatic property mapping capabilities that mirror the `.AutoMap()` functionality in regular projections. By following consistent naming conventions between events and read models, you can significantly reduce boilerplate code while maintaining type safety and performance. Use `FromEvent` as your starting point for model-bound projections, and add explicit attributes only when you need custom mapping logic or complex operations.

This automatic mapping approach makes model-bound projections both concise and maintainable, allowing you to focus on your domain logic rather than repetitive property mapping code.

