// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound;

public record AccountId(Guid Value);

public record AccountName(string Value);

public record AccountInfo(
    [Key, FromEventSourceId]
    AccountId Id,

    [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    AccountName Name,

    [AddFrom<DepositToDebitAccountPerformed>(nameof(DepositToDebitAccountPerformed.Amount))]
    [SubtractFrom<WithdrawalFromDebitAccountPerformed>(nameof(WithdrawalFromDebitAccountPerformed.Amount))]
    double Balance);

[FromEvent<DebitAccountOpened>]
public record AccountInfoWithClassLevelEvent(
    [Key, FromEventSourceId]
    AccountId Id,

    AccountName Name,

    [AddFrom<DepositToDebitAccountPerformed>(nameof(DepositToDebitAccountPerformed.Amount))]
    [SubtractFrom<WithdrawalFromDebitAccountPerformed>(nameof(WithdrawalFromDebitAccountPerformed.Amount))]
    double Balance);

public record CartItem([Key] string Id, string Name, double Price);

public record Cart(
    [Key, FromEventSourceId]
    Guid Id,

    [ChildrenFrom<ItemAddedToCart>(key: nameof(ItemAddedToCart.ItemId), identifiedBy: nameof(CartItem.Id))]
    IEnumerable<CartItem> Items);
