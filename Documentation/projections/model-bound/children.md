# Children Collections

Model-bound projections support child collections through the `ChildrenFrom` attribute, allowing you to build hierarchical read models with parent-child relationships.

## Basic Children

The `ChildrenFrom` attribute defines how child entities are added to a collection:

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record Order(
    [Key]
    Guid OrderId,

    [ChildrenFrom<LineItemAdded>(key: nameof(LineItemAdded.ItemId))]
    IEnumerable<LineItem> Items);

public record LineItem(
    [Key] Guid Id,  // Chronicle automatically discovers this as the key
    string ProductName,
    int Quantity,
    decimal Price);
```

In this example, the `[Key]` attribute on the `LineItem.Id` property is automatically discovered by Chronicle, so you don't need to specify `identifiedBy` explicitly in the `ChildrenFrom` attribute.

### Parameters

- **key** (optional): Property on the event that identifies the child. Defaults to `EventSourceId`
- **identifiedBy** (optional): Property on the child model that identifies it. If not specified, Chronicle will:
  1. Look for a property with the `[Key]` attribute
  2. Look for a property named `Id` (case-insensitive)
  3. Fall back to `EventSourceId` if neither is found
- **parentKey** (optional): Property that identifies the parent. Defaults to `EventSourceId`
  - Use this when the parent identifier is a property in the event content rather than the EventSourceId
  - Example: `parentKey: nameof(LineItemAdded.OrderId)` when OrderId is in the event
- **autoMap** (optional): Whether to automatically map matching properties from the event to the child model. Defaults to `true`

> **Note**: With automatic key discovery, you typically don't need to specify `identifiedBy` explicitly. Just mark your child model's key property with `[Key]` attribute, or name it `Id`, and Chronicle will automatically discover it.

### Auto-Mapping

By default, `ChildrenFrom` automatically maps properties from the event to the child model when property names match. This behavior is similar to the `FromEvent` attribute:

```csharp
public record Order(
    [Key]
    Guid OrderId,

    [ChildrenFrom<LineItemAdded>(key: nameof(LineItemAdded.ItemId))]
    IEnumerable<LineItem> Items);

public record LineItem(
    [Key] Guid Id,
    string ProductName,  // Automatically mapped from LineItemAdded.ProductName
    int Quantity,        // Automatically mapped from LineItemAdded.Quantity
    decimal Price);      // Automatically mapped from LineItemAdded.Price

[EventType]
public record LineItemAdded(
    Guid ItemId,
    string ProductName,
    int Quantity,
    decimal Price);
```

You can disable auto-mapping if you want to control property mapping explicitly:

```csharp
public record Order(
    [Key]
    Guid OrderId,

    [ChildrenFrom<LineItemAdded>(
        key: nameof(LineItemAdded.ItemId),
        autoMap: false)]
    IEnumerable<LineItem> Items);

public record LineItem(
    [Key] Guid Id,

    // Now you must use SetFrom for each property
    [SetFrom<LineItemAdded>(nameof(LineItemAdded.ProductName))]
    string ProductName,

    [SetFrom<LineItemAdded>(nameof(LineItemAdded.Quantity))]
    int Quantity,

    [SetFrom<LineItemAdded>(nameof(LineItemAdded.Price))]
    decimal Price);
```

## Recursive Attribute Processing

All projection attributes work recursively on child types. The child type's properties are automatically scanned for projection attributes:

```csharp
public record ShoppingCart(
    [Key]
    Guid CartId,

    [ChildrenFrom<ItemAddedToCart>(
        key: nameof(ItemAddedToCart.ItemId))]
    IEnumerable<CartItem> Items);

// Child type with its own projection attributes
public record CartItem(
    [Key] Guid Id,

    [SetFrom<ItemAddedToCart>(nameof(ItemAddedToCart.ProductName))]
    string ProductName,

    [SetFrom<ItemAddedToCart>(nameof(ItemAddedToCart.Price))]
    decimal Price,

    [SetFrom<ItemAddedToCart>(nameof(ItemAddedToCart.InitialQuantity))]
    [Increment<QuantityIncreased>]
    [Decrement<QuantityDecreased>]
    int Quantity);
```

When an `ItemAddedToCart` event occurs:

1. A new `CartItem` is added to the collection
2. Properties are mapped from the event to the child
3. The child's own attributes are processed

When a `QuantityIncreased` event occurs later:

- The projection finds the matching child by ID
- Increments the `Quantity` on that specific child

## Removing Children

Use `RemovedWith` to remove children from collections. You can apply it either on the collection property or on the child type class:

### Property-Level Removal

```csharp
public record Order(
    [Key]
    Guid OrderId,

    [ChildrenFrom<LineItemAdded>(key: nameof(LineItemAdded.ItemId))]
    [RemovedWith<LineItemRemoved>(key: nameof(LineItemRemoved.ItemId))]
    IEnumerable<OrderLine> Lines);

public record OrderLine(
    [Key] Guid Id,
    string Description);
```

### Class-Level Removal

Apply `RemovedWith` directly on the child type for better separation of concerns:

```csharp
public record Order(
    [Key]
    Guid OrderId,

    [ChildrenFrom<LineItemAdded>(key: nameof(LineItemAdded.ItemId))]
    IEnumerable<OrderLine> Lines);

[RemovedWith<LineItemRemoved>(
    key: nameof(LineItemRemoved.ItemId),
    parentKey: nameof(LineItemRemoved.OrderId))]
public record OrderLine(
    [Key] Guid Id,
    string Description);
```

### RemovedWithJoin

For removal based on events from different streams (joins), use `RemovedWithJoin`:

```csharp
public record Subscription(
    [Key]
    Guid SubscriptionId,

    [ChildrenFrom<FeatureActivated>]
    [RemovedWithJoin<FeatureDeactivated>(key: nameof(FeatureDeactivated.FeatureId))]
    IEnumerable<Feature> Features);
```

Or at the class level:

```csharp
[RemovedWithJoin<FeatureDeactivated>(key: nameof(FeatureDeactivated.FeatureId))]
public record Feature(
    [Key] Guid FeatureId,
    string Name);
```

> **Note**: For comprehensive documentation on removal options including removing root read models, see [Removal](./removal.md).

## Complete Example

Here's a comprehensive example showing children with full attribute support:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

// Events
[EventType]
public record OrderCreated(string CustomerName);

[EventType]
public record LineItemAdded(
    Guid ItemId,
    string ProductName,
    int InitialQuantity,
    decimal UnitPrice);

[EventType]
public record QuantityAdjusted(Guid ItemId, int NewQuantity);

[EventType]
public record LineItemRemoved(Guid ItemId);

// Read Models
public record Order(
    [Key]
    Guid Id,

    [SetFrom<OrderCreated>(nameof(OrderCreated.CustomerName))]
    string Customer,

    [ChildrenFrom<LineItemAdded>(key: nameof(LineItemAdded.ItemId))]
    [RemovedWith<LineItemRemoved>(key: nameof(LineItemRemoved.ItemId))]
    IEnumerable<OrderLine> Lines);

public record OrderLine(
    [Key] Guid Id,

    [SetFrom<LineItemAdded>(nameof(LineItemAdded.ProductName))]
    string Product,

    [SetFrom<LineItemAdded>(nameof(LineItemAdded.InitialQuantity))]
    [SetFrom<QuantityAdjusted>(nameof(QuantityAdjusted.NewQuantity))]
    int Quantity,

    [SetFrom<LineItemAdded>(nameof(LineItemAdded.UnitPrice))]
    decimal UnitPrice);
```

## Event Processing Flow

1. **OrderCreated** - Creates the parent Order with customer name
2. **LineItemAdded** - Adds a new OrderLine to the collection with initial values
3. **QuantityAdjusted** - Updates the Quantity on the matching OrderLine
4. **LineItemRemoved** - Removes the OrderLine from the collection

## Nested Children

Children can have their own children, creating deeply nested structures. All projection attributes (joins, removal, counters, context mapping, etc.) work recursively at every level:

```csharp
// Events
[EventType]
public record OrganizationCreated(string Name);

[EventType]
public record DepartmentAdded(DepartmentId Id, string Name);

[EventType]
public record DepartmentRenamed(DepartmentId Id, string NewName);

[EventType]
public record TeamAdded(TeamId Id, DepartmentId DepartmentId, string Name);

[EventType]
public record TeamRenamed(TeamId Id, string NewName);

[EventType]
public record MemberAdded(MemberId Id, TeamId TeamId, string Name);

[EventType]
public record MemberRemoved(MemberId Id, TeamId TeamId);

// Read Models - all attributes work at every nesting level
public record Organization(
    [Key] Guid Id,

    [SetFrom<OrganizationCreated>]
    string Name,

    [ChildrenFrom<DepartmentAdded>(
        key: nameof(DepartmentAdded.Id),
        identifiedBy: nameof(Department.Id))]
    IEnumerable<Department> Departments);

public record Department(
    [Key] DepartmentId Id,

    [SetFrom<DepartmentAdded>]
    [Join<DepartmentRenamed>(nameof(DepartmentRenamed.Id))]  // Joins work on children
    string Name,

    [ChildrenFrom<TeamAdded>(
        key: nameof(TeamAdded.Id),
        identifiedBy: nameof(Team.Id),
        parentKey: nameof(TeamAdded.DepartmentId))]  // Nested children
    IEnumerable<Team> Teams);

public record Team(
    [Key] TeamId Id,

    [SetFrom<TeamAdded>]
    [Join<TeamRenamed>(nameof(TeamRenamed.Id))]  // Joins work on nested children too
    string Name,

    [ChildrenFrom<MemberAdded>(
        key: nameof(MemberAdded.Id),
        identifiedBy: nameof(Member.Id),
        parentKey: nameof(MemberAdded.TeamId))]
    [RemovedWith<MemberRemoved>(key: nameof(MemberRemoved.Id))]  // Removal works at all levels
    IEnumerable<Member> Members);

public record Member(
    [Key] MemberId Id,

    [SetFrom<MemberAdded>]
    string Name);
```

### What Works Recursively

All projection attributes are fully supported on child types at any nesting level:

| Attribute | Works on Children |
|-----------|-------------------|
| `SetFrom` | ✓ |
| `AddFrom` / `SubtractFrom` | ✓ |
| `SetFromContext` | ✓ |
| `Join` | ✓ |
| `Increment` / `Decrement` / `Count` | ✓ |
| `ChildrenFrom` (nested children) | ✓ |
| `RemovedWith` / `RemovedWithJoin` | ✓ |
| `FromEvent` (class-level) | ✓ |

This means you can build arbitrarily deep hierarchies with full projection capabilities at every level.

## Best Practices

1. **Always use Key attribute** on child types to identify them uniquely
2. **Leverage recursive attributes** to build complex child projections without duplication
3. **Use RemovedWith** to maintain collection integrity when items are removed
4. **Consider performance** with large collections - deeply nested structures can impact query performance
