# Model-Bound Projections Example

This example demonstrates how to use attribute-based projections in Chronicle.

## Simple Projection with SetFrom

```csharp
using Cratis.Chronicle.Projections.ModelBound;

[ReadModel]
public record AccountInfo(
    [ModelKey, FromEventSourceId]
    AccountId Id,

    [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    AccountName Name,

    [AddFrom<DepositToDebitAccountPerformed>(nameof(DepositToDebitAccountPerformed.Amount))]
    [SubtractFrom<WithdrawalFromDebitAccountPerformed>(nameof(WithdrawalFromDebitAccountPerformed.Amount))]
    double Balance);
```

## Convention-Based Mapping with FromEvent

```csharp
[ReadModel]
[FromEvent<DebitAccountOpened>]
public record AccountInfoWithClassLevelEvent(
    [ModelKey, FromEventSourceId]
    AccountId Id,

    AccountName Name,  // Automatically mapped from DebitAccountOpened.Name

    [AddFrom<DepositToDebitAccountPerformed>(nameof(DepositToDebitAccountPerformed.Amount))]
    [SubtractFrom<WithdrawalFromDebitAccountPerformed>(nameof(WithdrawalFromDebitAccountPerformed.Amount))]
    double Balance);
```

## Projection with Children

```csharp
[ReadModel]
public record Cart(
    [ModelKey, FromEventSourceId]
    Guid Id,

    [ChildrenFrom<ItemAddedToCart>(
        key: nameof(ItemAddedToCart.ItemId), 
        identifiedBy: nameof(CartItem.Id))]
    IEnumerable<CartItem> Items);

public record CartItem(string Id, string Name, double Price);
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

1. The `ReadModelAttribute` marks a type for automatic projection discovery
2. Property attributes define how event properties map to model properties
3. The projection system automatically:
   - Discovers all types with `ReadModelAttribute`
   - Processes the attributes to build projection definitions
   - Registers the projections alongside fluent projections

No need to implement `IProjectionFor<T>` - just decorate your models!
