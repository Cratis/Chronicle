// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections.ModelBound;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

public record AccountId(Guid Value);

public record AccountName(string Value);

public record AccountInfo(
    [Key]
    AccountId Id,

    [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    AccountName Name,

    [AddFrom<DepositToDebitAccountPerformed>(nameof(DepositToDebitAccountPerformed.Amount))]
    [SubtractFrom<WithdrawalFromDebitAccountPerformed>(nameof(WithdrawalFromDebitAccountPerformed.Amount))]
    double Balance);

[FromEvent<DebitAccountOpened>]
public record AccountInfoWithClassLevelEvent(
    [Key]
    AccountId Id,

    AccountName Name,

    [AddFrom<DepositToDebitAccountPerformed>(nameof(DepositToDebitAccountPerformed.Amount))]
    [SubtractFrom<WithdrawalFromDebitAccountPerformed>(nameof(WithdrawalFromDebitAccountPerformed.Amount))]
    double Balance);

public record CartItem([Key] string Id, string Name, double Price);

public record Cart(
    [Key]
    Guid Id,

    [ChildrenFrom<ItemAddedToCart>(key: nameof(ItemAddedToCart.ItemId), identifiedBy: nameof(CartItem.Id))]
    IEnumerable<CartItem> Items);

[FromEventSequence("audit-log")]
public record AuditRecord(
    [Key]
    Guid Id,

    [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    string EventName);

[NotRewindable]
public record AuditLogEntry(
    [Key]
    Guid Id,

    [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    string Message);

[Passive]
public record Snapshot(
    [Key]
    Guid Id,

    [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    string Data);

[FromEventSequence("custom")]
[NotRewindable]
[Passive]
public record ConfiguredProjection(
    [Key]
    Guid Id,

    [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    string Value);

public record ProductInfo(
    [Key]
    Guid Id,

    [SetFrom<ProductRegistered>(nameof(ProductRegistered.Name))]
    string Name,

    [SetFrom<ProductRegistered>(nameof(ProductRegistered.Description))]
    string Description,

    [SetFrom<ProductRegistered>(nameof(ProductRegistered.Price))]
    double Price);

public record OrderSummary(
    [Key]
    Guid Id,

    [SetFrom<OrderPlaced>(nameof(OrderPlaced.CustomerName))]
    string CustomerName,

    [AddFrom<OrderPlaced>(nameof(OrderPlaced.Quantity))]
    int TotalQuantity,

    [AddFrom<OrderPlaced>(nameof(OrderPlaced.TotalAmount))]
    double TotalRevenue);

[FromEvent<ProductRegisteredInInventory>]
public record InventoryStatus(
    [Key]
    Guid Id,

    string ProductName,

    [AddFrom<ItemsAddedToInventory>(nameof(ItemsAddedToInventory.Quantity))]
    [SubtractFrom<ItemsRemovedFromInventory>(nameof(ItemsRemovedFromInventory.Quantity))]
    int CurrentStock,

    [FromEvery(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset LastUpdated);

[FromEvent<UserRegisteredWithCustomId>(key: nameof(UserRegisteredWithCustomId.UserId))]
public record UserProfile(
    [Key]
    Guid Id,

    string Email,

    string Name);

[RemovedWith<ReadModelRemoved>]
public record RemovableReadModel(
    [Key]
    Guid Id,

    [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    string Name);

[RemovedWith<ReadModelRemoved>]
[RemovedWithJoin<ReadModelRemovedJoin>]
public record ReadModelWithMultipleRemovalOptions(
    [Key]
    Guid Id,

    [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    string Name);

[RemovedWith<ReadModelRemoved>(key: nameof(ReadModelRemoved.Id))]
public record RemovableReadModelWithKey(
    [Key]
    Guid Id,

    [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    string Name);

[RemovedWith<ChildItemRemoved>(key: nameof(ChildItemRemoved.ItemId), parentKey: nameof(ChildItemRemoved.ParentId))]
public record RemovableChildItem(
    [Key]
    Guid Id,

    string Name);

public record ParentWithRemovableChildren(
    [Key]
    Guid Id,

    [ChildrenFrom<ItemAddedToCart>(key: nameof(ItemAddedToCart.ItemId), identifiedBy: nameof(RemovableChildItem.Id))]
    IEnumerable<RemovableChildItem> Items);

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
