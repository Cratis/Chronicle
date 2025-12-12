# Projection AutoMap

AutoMap is a powerful feature that automatically maps properties with matching names between events and read models. This eliminates the need for explicit property mappings when property names and types are compatible.

## Basic AutoMap usage

At the top level, `AutoMap()` automatically maps all properties from events to the read model:

```csharp
using Cratis.Chronicle.Projections;

public class UserProjection : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .AutoMap()
        .From<UserCreated>()
        .From<UserUpdated>();
}
```

This automatically maps all properties with matching names from both `UserCreated` and `UserUpdated` events to the `User` read model.

## How AutoMap works

AutoMap performs name-based matching:

1. **Property name matching**: Looks for properties with identical names (case-sensitive) in both event and read model
2. **Type compatibility**: Ensures the property types are compatible for assignment
3. **Recursive mapping**: For nested objects, AutoMap recursively maps nested properties
4. **Collection handling**: Arrays and collections are automatically handled

Example:

```csharp
// Event
[EventType]
public record UserCreated(string Name, string Email, Address HomeAddress);

// Nested type
public record Address(string Street, string City, string ZipCode);

// Read model
public record User(string Name, string Email, Address HomeAddress);
```

With `AutoMap()`, all properties including the nested `Address` object are automatically mapped.

## AutoMap at different levels

AutoMap can be applied at three different levels in a projection:

### 1. Top-level AutoMap

Apply to the entire projection - affects all event handlers:

```csharp
public void Define(IProjectionBuilderFor<Account> builder) => builder
    .AutoMap()  // Applies to all From() and Join() calls
    .From<AccountOpened>()
    .From<AccountUpdated>()
    .Join<CustomerUpdated>(j => j.On(m => m.CustomerId));
```

### 2. Per-event AutoMap

Apply to specific event handlers:

```csharp
public void Define(IProjectionBuilderFor<Account> builder) => builder
    .From<AccountOpened>(_ => _.AutoMap())  // Only this event uses AutoMap
    .From<AccountClosed>(_ => _
        .Set(m => m.IsActive).To(false)
        .Set(m => m.ClosedAt).ToEventContextProperty(c => c.Occurred));
```

### 3. Children AutoMap

Apply to child collection projections. When using top-level `AutoMap()`, it automatically cascades to children:

```csharp
public void Define(IProjectionBuilderFor<Order> builder) => builder
    .AutoMap()  // Cascades to children
    .From<OrderCreated>()
    .Children(m => m.Items, children => children
        .IdentifiedBy(e => e.ProductId)
        // AutoMap is inherited from parent
        .From<ItemAddedToOrder>(_ => _
            .UsingKey(e => e.ProductId)));
```

Or apply AutoMap explicitly at the child level:

```csharp
public void Define(IProjectionBuilderFor<Order> builder) => builder
    .From<OrderCreated>(_ => _
        .Set(m => m.OrderNumber).To(e => e.Number))
    .Children(m => m.Items, children => children
        .IdentifiedBy(e => e.ProductId)
        .AutoMap()  // Explicit AutoMap for children only
        .From<ItemAddedToOrder>(_ => _
            .UsingKey(e => e.ProductId)));
```

## AutoMap with joins

AutoMap works with joins to automatically map properties from joined events:

```csharp
public void Define(IProjectionBuilderFor<Employee> builder) => builder
    .AutoMap()
    .From<EmployeeHired>()
    .From<EmployeeAssignedToDepartment>(_ => _
        .UsingKey(e => e.EmployeeId)
        .Set(m => m.DepartmentId).ToEventSourceId())
    .Join<DepartmentCreated>(j => j
        .On(m => m.DepartmentId));  // AutoMap applies to joined properties
```

When `DepartmentCreated` has properties like `DepartmentName` and `DepartmentCode`, and the `Employee` read model has matching properties, they are automatically mapped.

## AutoMap in child joins

AutoMap can be applied to joins within child collections:

```csharp
public void Define(IProjectionBuilderFor<Project> builder) => builder
    .AutoMap()
    .From<ProjectCreated>()
    .Children(m => m.TeamMembers, children => children
        .IdentifiedBy(e => e.EmployeeId)
        .AutoMap()
        .From<EmployeeAssignedToProject>(_ => _
            .UsingKey(e => e.EmployeeId))
        .Join<EmployeeProfileUpdated>(j => j
            .On(m => m.EmployeeId)));  // AutoMap applies here too
```

## Combining AutoMap with explicit mappings

AutoMap and explicit mappings work together seamlessly. AutoMap handles matching properties, while explicit mappings handle special cases:

```csharp
public void Define(IProjectionBuilderFor<Account> builder) => builder
    .AutoMap()  // Maps matching properties automatically
    .From<AccountOpened>(_ => _
        // Explicit mappings override/extend AutoMap
        .Set(m => m.Status).To("Active")
        .Set(m => m.Balance).To(0m)
        .Set(m => m.CreatedAt).ToEventContextProperty(c => c.Occurred))
    .From<MoneyDeposited>();  // Uses only AutoMap
```

In this example:

- `AccountOpened` uses AutoMap for matching properties, plus explicit mappings for `Status`, `Balance`, and `CreatedAt`
- `MoneyDeposited` relies entirely on AutoMap

## AutoMap with nested children

AutoMap cascades through multiple levels of hierarchy:

```csharp
public void Define(IProjectionBuilderFor<Company> builder) => builder
    .AutoMap()  // Cascades to all levels
    .From<CompanyRegistered>()
    .Children(m => m.Departments, departments => departments
        .IdentifiedBy(e => e.DepartmentId)
        // AutoMap inherited from parent
        .From<DepartmentCreated>(_ => _
            .UsingKey(e => e.DepartmentId))
        // No parent key specified - uses EventSourceId (CompanyId) by default
        .Children(dm => dm.Employees, employees => employees
            .IdentifiedBy(e => e.EmployeeId)
            // AutoMap still applies at this nested level
            .From<EmployeeAssignedToDepartment>(_ => _
                .UsingParentKey(e => e.DepartmentId)  // Extracts from event content
                .UsingKey(e => e.EmployeeId))));
```

## When to use AutoMap

**Use AutoMap when:**

- Property names match exactly between events and read models
- Types are directly compatible
- You want to reduce boilerplate code
- You're following naming conventions consistently

**Avoid AutoMap when:**

- Property names differ between events and read models
- Complex transformations are needed
- You need fine-grained control over mapping logic
- Properties require calculations or aggregations

## Best practices

1. **Consistent naming**: Use consistent property names across events and read models to maximize AutoMap effectiveness
2. **Combine approaches**: Use AutoMap for simple mappings and explicit `Set()` calls for complex transformations
3. **Be explicit when needed**: If clarity matters more than brevity, use explicit mappings even when AutoMap would work
4. **Document custom logic**: When mixing AutoMap with explicit mappings, document why specific properties need custom handling
5. **Nested structures**: Ensure nested types also follow consistent naming for recursive AutoMap to work effectively

## Performance considerations

AutoMap performs name matching at projection definition time, not during event processing. There is no runtime performance penalty for using AutoMap compared to explicit mappings - both compile to the same internal representation.

## Examples

### Simple AutoMap

```csharp
public class ProductProjection : IProjectionFor<Product>
{
    public void Define(IProjectionBuilderFor<Product> builder) => builder
        .AutoMap()
        .From<ProductCreated>()
        .From<ProductUpdated>();
}
```

### AutoMap with children

```csharp
public class OrderProjection : IProjectionFor<Order>
{
    public void Define(IProjectionBuilderFor<Order> builder) => builder
        .AutoMap()
        .From<OrderPlaced>()
        .Children(m => m.Items, children => children
            .IdentifiedBy(e => e.LineItemId)
            .From<LineItemAdded>(_ => _
                .UsingKey(e => e.LineItemId)));
}
```

### AutoMap with explicit overrides

```csharp
public class CustomerProjection : IProjectionFor<Customer>
{
    public void Define(IProjectionBuilderFor<Customer> builder) => builder
        .AutoMap()
        .From<CustomerRegistered>(_ => _
            .Set(m => m.Status).To("Active")
            .Set(m => m.MemberSince).ToEventContextProperty(c => c.Occurred))
        .From<CustomerDetailsUpdated>();  // Pure AutoMap
}
```

### Selective AutoMap for children only

```csharp
public class TeamProjection : IProjectionFor<Team>
{
    public void Define(IProjectionBuilderFor<Team> builder) => builder
        .From<TeamFormed>(_ => _
            .Set(m => m.Name).To(e => e.TeamName)
            .Set(m => m.CreatedAt).ToEventContextProperty(c => c.Occurred))
        .Children(m => m.Members, children => children
            .IdentifiedBy(e => e.MemberId)
            .AutoMap()  // Only children use AutoMap
            .From<MemberJoinedTeam>(_ => _
                .UsingKey(e => e.MemberId)));
}
```
