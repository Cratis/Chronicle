// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

public record AccountId(Guid Value);

public record AccountName(string Value);

[ReadModel]
public record AccountInfo(
    [ModelKey, FromEventSourceId]
    AccountId Id,

    [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    AccountName Name,

    [AddFrom<DepositToDebitAccountPerformed>(nameof(DepositToDebitAccountPerformed.Amount))]
    [SubtractFrom<WithdrawalFromDebitAccountPerformed>(nameof(WithdrawalFromDebitAccountPerformed.Amount))]
    double Balance);

[ReadModel]
[FromEvent<DebitAccountOpened>]
public record AccountInfoWithClassLevelEvent(
    [ModelKey, FromEventSourceId]
    AccountId Id,

    AccountName Name,

    [AddFrom<DepositToDebitAccountPerformed>(nameof(DepositToDebitAccountPerformed.Amount))]
    [SubtractFrom<WithdrawalFromDebitAccountPerformed>(nameof(WithdrawalFromDebitAccountPerformed.Amount))]
    double Balance);

public record CartItem(string Id, string Name, double Price);

[ReadModel]
public record Cart(
    [ModelKey, FromEventSourceId]
    Guid Id,

    [ChildrenFrom<ItemAddedToCart>(key: nameof(ItemAddedToCart.ItemId), identifiedBy: nameof(CartItem.Id))]
    IEnumerable<CartItem> Items);
