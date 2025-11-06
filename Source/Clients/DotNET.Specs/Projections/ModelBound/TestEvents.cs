// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
[EventType]
public record DebitAccountOpened(string Name, double InitialBalance);

[EventType]
public record DepositToDebitAccountPerformed(double Amount);

[EventType]
public record WithdrawalFromDebitAccountPerformed(double Amount);

[EventType]
public record ItemAddedToCart(string ItemId, string ItemName, double Price);

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
