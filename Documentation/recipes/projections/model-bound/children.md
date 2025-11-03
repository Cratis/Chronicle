# Children Collections

Model-bound projections support child collections through the `ChildrenFrom` attribute, allowing you to build hierarchical read models with parent-child relationships.

## Basic Children

The `ChildrenFrom` attribute defines how child entities are added to a collection:

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record Order(
    [Key, FromEventSourceId]
    Guid OrderId,
    
    [ChildrenFrom<LineItemAdded>(
        key: nameof(LineItemAdded.ItemId),
        identifiedBy: nameof(LineItem.Id))]
    IEnumerable<LineItem> Items);

public record LineItem(
    [Key] Guid Id,
    string ProductName,
    int Quantity,
    decimal Price);
```

### Parameters

- **key** (optional): Property on the event that identifies the child. Defaults to `EventSourceId`
- **identifiedBy** (optional): Property on the child model that identifies it. Defaults to `Id`
- **parentKey** (optional): Property that identifies the parent. Defaults to `EventSourceId`

## Recursive Attribute Processing

All projection attributes work recursively on child types. The child type's properties are automatically scanned for projection attributes:

```csharp
public record ShoppingCart(
    [Key, FromEventSourceId]
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

Use `RemovedWith` to remove children from collections:

```csharp
public record Order(
    [Key, FromEventSourceId]
    Guid OrderId,
    
    [ChildrenFrom<LineItemAdded>(key: nameof(LineItemAdded.ItemId))]
    [RemovedWith<LineItemRemoved>(key: nameof(LineItemRemoved.ItemId))]
    IEnumerable<OrderLine> Lines);

public record OrderLine(
    [Key] Guid Id,
    string Description);
```

### RemovedWithJoin

For more complex removal scenarios using joins:

```csharp
public record Subscription(
    [Key, FromEventSourceId]
    Guid SubscriptionId,
    
    [ChildrenFrom<FeatureActivated>]
    [RemovedWithJoin<FeatureDeactivated>(key: nameof(FeatureDeactivated.FeatureId))]
    IEnumerable<Feature> Features);
```

## Complete Example

Here's a comprehensive example showing children with full attribute support:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

// Events
[EventType("order-created")]
public record OrderCreated(string CustomerName);

[EventType("line-item-added")]
public record LineItemAdded(
    Guid ItemId,
    string ProductName,
    int InitialQuantity,
    decimal UnitPrice);

[EventType("quantity-adjusted")]
public record QuantityAdjusted(Guid ItemId, int NewQuantity);

[EventType("line-item-removed")]
public record LineItemRemoved(Guid ItemId);

// Read Models
public record Order(
    [Key, FromEventSourceId]
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

Children can have their own children, creating deeply nested structures:

```csharp
public record Organization(
    [Key, FromEventSourceId] Guid Id,
    [ChildrenFrom<DepartmentCreated>] IEnumerable<Department> Departments);

public record Department(
    [Key] Guid Id,
    [SetFrom<DepartmentCreated>] string Name,
    [ChildrenFrom<TeamCreated>] IEnumerable<Team> Teams);

public record Team(
    [Key] Guid Id,
    [SetFrom<TeamCreated>] string Name,
    [ChildrenFrom<MemberAdded>] IEnumerable<Member> Members);

public record Member(
    [Key] Guid Id,
    [SetFrom<MemberAdded>] string Name);
```

All attributes are processed recursively at every level of the hierarchy.

## Best Practices

1. **Always use Key attribute** on child types to identify them uniquely
2. **Leverage recursive attributes** to build complex child projections without duplication
3. **Use RemovedWith** to maintain collection integrity when items are removed
4. **Consider performance** with large collections - deeply nested structures can impact query performance
