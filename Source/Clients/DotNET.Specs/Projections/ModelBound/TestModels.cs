// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

public record AccountId(Guid Value);

public record AccountName(string Value);

public record AccountInfo(
    [property: Key] [property: FromEventSourceId]
    AccountId Id,

    [property: SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    AccountName Name,

    [property: AddFrom<DepositToDebitAccountPerformed>(nameof(DepositToDebitAccountPerformed.Amount))]
    [property: SubtractFrom<WithdrawalFromDebitAccountPerformed>(nameof(WithdrawalFromDebitAccountPerformed.Amount))]
    double Balance);

[FromEvent<DebitAccountOpened>]
public record AccountInfoWithClassLevelEvent(
    [property: Key] [property: FromEventSourceId]
    AccountId Id,

    AccountName Name,

    [property: AddFrom<DepositToDebitAccountPerformed>(nameof(DepositToDebitAccountPerformed.Amount))]
    [property: SubtractFrom<WithdrawalFromDebitAccountPerformed>(nameof(WithdrawalFromDebitAccountPerformed.Amount))]
    double Balance);

public record CartItem([property: Key] string Id, string Name, double Price);

public record Cart(
    [property: Key] [property: FromEventSourceId]
    Guid Id,

    [property: ChildrenFrom<ItemAddedToCart>(key: nameof(ItemAddedToCart.ItemId), identifiedBy: nameof(CartItem.Id))]
    IEnumerable<CartItem> Items);

[FromEventSequence("audit-log")]
public record AuditRecord(
    [property: Key] [property: FromEventSourceId]
    Guid Id,

    [property: SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    string EventName);

[NotRewindable]
public record AuditLogEntry(
    [property: Key] [property: FromEventSourceId]
    Guid Id,

    [property: SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    string Message);

[Passive]
public record Snapshot(
    [property: Key] [property: FromEventSourceId]
    Guid Id,

    [property: SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    string Data);

[FromEventSequence("custom")]
[NotRewindable]
[Passive]
public record ConfiguredProjection(
    [property: Key] [property: FromEventSourceId]
    Guid Id,

    [property: SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
    string Value);

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
