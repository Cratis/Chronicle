# Removing Read Models

Model-bound projections support removing read models and child items through the `RemovedWith` and `RemovedWithJoin` attributes. These can be applied at both the class level (for root read models and child types) and on properties/parameters (for child collections).

## Removing Root Read Models

Use `RemovedWith` at the class level to specify which event removes the entire read model instance:

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[RemovedWith<AccountClosed>]
public record Account(
    [Key]
    Guid Id,

    [SetFrom<AccountOpened>(nameof(AccountOpened.Name))]
    string Name,

    [SetFrom<AccountOpened>(nameof(AccountOpened.Balance))]
    decimal Balance);
```

When an `AccountClosed` event occurs, the corresponding `Account` read model is removed from the store.

### With Custom Key

You can specify which property on the event identifies the read model to remove:

```csharp
[RemovedWith<AccountClosed>(key: nameof(AccountClosed.AccountId))]
public record Account(
    [Key]
    Guid Id,

    [SetFrom<AccountOpened>(nameof(AccountOpened.Name))]
    string Name);
```

## Multiple Removal Options

A read model can be removed by multiple different events:

```csharp
[RemovedWith<AccountClosed>]
[RemovedWith<AccountMerged>(key: nameof(AccountMerged.SourceAccountId))]
[RemovedWithJoin<OrganizationClosed>]
public record Account(
    [Key]
    Guid Id,

    [SetFrom<AccountOpened>(nameof(AccountOpened.Name))]
    string Name);
```

In this example, an account can be removed by:

- An `AccountClosed` event (direct removal)
- An `AccountMerged` event when it's the source account
- An `OrganizationClosed` event through a join relationship

## RemovedWithJoin

Use `RemovedWithJoin` when the removal event comes from a different stream (join relationship):

```csharp
[RemovedWithJoin<CompanyDissolved>]
public record Employee(
    [Key]
    Guid Id,

    [SetFrom<EmployeeHired>(nameof(EmployeeHired.Name))]
    string Name,

    [Join<CompanyRegistered>]
    string CompanyName);
```

When the company is dissolved, all employees associated with that company are removed.

## Removing Children

Children can be removed in two ways:

### Property-Level Removal

Apply `RemovedWith` on the collection property alongside `ChildrenFrom`:

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

### Class-Level Removal on Child Types

Apply `RemovedWith` directly on the child type. This is particularly useful when the same child model is used in multiple parents or when you want to keep removal logic with the child definition:

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

Both approaches produce the same result. The class-level approach keeps the removal definition with the child type, while the property-level approach keeps it with the parent.

### Parameters

For child removal, you can specify:

- **key**: Property on the event that identifies which child to remove
- **parentKey**: Property on the event that identifies the parent (defaults to EventSourceId)

## Children with RemovedWithJoin

For children that should be removed based on join events:

```csharp
public record UserProfile(
    [Key]
    Guid UserId,

    [ChildrenFrom<UserJoinedGroup>(key: nameof(UserJoinedGroup.GroupId))]
    [RemovedWithJoin<GroupDeleted>]
    IEnumerable<GroupMembership> Groups);
```

Or at the class level:

```csharp
public record UserProfile(
    [Key]
    Guid UserId,

    [ChildrenFrom<UserJoinedGroup>(key: nameof(UserJoinedGroup.GroupId))]
    IEnumerable<GroupMembership> Groups);

[RemovedWithJoin<GroupDeleted>(key: nameof(GroupDeleted.GroupId))]
public record GroupMembership(
    [Key] Guid GroupId,
    string GroupName);
```

## Complete Example

Here's a comprehensive example showing both root and child removal:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

// Events
[EventType]
public record ShoppingCartCreated(string CustomerName);

[EventType]
public record ItemAddedToCart(Guid ItemId, string ProductName, decimal Price);

[EventType]
public record ItemRemovedFromCart(Guid CartId, Guid ItemId);

[EventType]
public record CartCheckedOut();

[EventType]
public record CartAbandoned();

// Read Models
[RemovedWith<CartCheckedOut>]
[RemovedWith<CartAbandoned>]
public record ShoppingCart(
    [Key]
    Guid Id,

    [SetFrom<ShoppingCartCreated>(nameof(ShoppingCartCreated.CustomerName))]
    string Customer,

    [ChildrenFrom<ItemAddedToCart>(key: nameof(ItemAddedToCart.ItemId))]
    IEnumerable<CartItem> Items);

[RemovedWith<ItemRemovedFromCart>(
    key: nameof(ItemRemovedFromCart.ItemId),
    parentKey: nameof(ItemRemovedFromCart.CartId))]
public record CartItem(
    [Key] Guid Id,

    [SetFrom<ItemAddedToCart>(nameof(ItemAddedToCart.ProductName))]
    string Product,

    [SetFrom<ItemAddedToCart>(nameof(ItemAddedToCart.Price))]
    decimal Price);
```

## Best Practices

1. **Use class-level removal** for root read models to keep the removal logic with the model definition
2. **Choose property vs class-level removal for children** based on where the logic fits best:
   - Property-level if the removal is specific to how the child is used in that parent
   - Class-level if the removal logic applies universally to that child type
3. **Always specify keys explicitly** when the default EventSourceId doesn't apply
4. **Use RemovedWithJoin** for removal events from different streams (e.g., when a parent entity in another aggregate is deleted)
5. **Combine multiple removal attributes** when a model can be removed by different events
