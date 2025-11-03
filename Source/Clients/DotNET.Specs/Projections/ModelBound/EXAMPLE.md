# Model-Bound Projections Example

This example demonstrates how to use attribute-based projections in Chronicle.

## Simple Projection with SetFrom

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record AccountInfo(
    [Key, FromEventSourceId]
    AccountId Id,

    [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    AccountName Name,

    [AddFrom<DepositToDebitAccountPerformed>(nameof(DepositToDebitAccountPerformed.Amount))]
    [SubtractFrom<WithdrawalFromDebitAccountPerformed>(nameof(WithdrawalFromDebitAccountPerformed.Amount))]
    double Balance);
```

## Convention-Based Mapping with FromEvent

```csharp
[FromEvent<DebitAccountOpened>]
public record AccountInfoWithClassLevelEvent(
    [Key, FromEventSourceId]
    AccountId Id,

    AccountName Name,  // Automatically mapped from DebitAccountOpened.Name

    [AddFrom<DepositToDebitAccountPerformed>(nameof(DepositToDebitAccountPerformed.Amount))]
    [SubtractFrom<WithdrawalFromDebitAccountPerformed>(nameof(WithdrawalFromDebitAccountPerformed.Amount))]
    double Balance);
```

## Projection with Children

```csharp
public record Cart(
    [Key, FromEventSourceId]
    Guid Id,

    [ChildrenFrom<ItemAddedToCart>(
        key: nameof(ItemAddedToCart.ItemId), 
        identifiedBy: nameof(CartItem.Id))]
    IEnumerable<CartItem> Items);

public record CartItem([Key] string Id, string Name, double Price);
```

## Additional Operations

### Increment and Decrement

```csharp
public record Statistics(
    [Key] Guid Id,
    
    [Increment<UserLoggedIn>]
    int LoginCount,
    
    [Decrement<UserLoggedOut>]
    int ActiveUsers);
```

### Count

```csharp
public record EventCounter(
    [Key] Guid Id,
    
    [Count<SomeEvent>]
    int EventCount);
```

### Join

```csharp
public record OrderWithCustomer(
    [Key] Guid OrderId,
    
    [Join<CustomerCreated>(on: nameof(CustomerId))]
    string CustomerName);
```

## Corresponding Events

```csharp
using Cratis.Chronicle.Events;

[EventType("31b5c5c4-5b8a-4c4f-8c4f-1b8a5c4c5c4f")]
public record DebitAccountOpened(string Name, double InitialBalance);

[EventType("41b5c5c4-5b8a-4c4f-8c4f-1b8a5c4c5c4f")]
public record DepositToDebitAccountPerformed(double Amount);

[EventType("51b5c5c4-5b8a-4c4f-8c4f-1b8a5c4c5c4f")]
public record WithdrawalFromDebitAccountPerformed(double Amount);

[EventType("61b5c5c4-5b8a-4c4f-8c4f-1b8a5c4c5c4f")]
public record ItemAddedToCart(string ItemId, string ItemName, double Price);
```

## How It Works

1. The `KeyAttribute` from `Cratis.Chronicle.Keys` identifies the key property
2. Property attributes define how event properties map to model properties
3. The projection system automatically:
   - Discovers all types with projection attributes (SetFrom, AddFrom, etc.)
   - Processes the attributes to build projection definitions
   - Registers the projections alongside fluent projections

No need to implement `IProjectionFor<T>` - just decorate your models!

## Recursive Support

All attributes work recursively on children and joined types:

```csharp
public record Order(
    [Key] Guid Id,
    
    [ChildrenFrom<LineItemAdded>]
    IEnumerable<LineItem> Items);

// Attributes on child types are processed automatically
public record LineItem(
    [Key] Guid Id,
    
    [SetFrom<LineItemAdded>]
    string ProductName,
    
    [Increment<QuantityIncreased>]
    [Decrement<QuantityDecreased>]
    int Quantity);
```
