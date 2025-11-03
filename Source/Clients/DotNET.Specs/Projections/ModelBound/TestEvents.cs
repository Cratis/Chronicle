// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound;

[EventType("31b5c5c4-5b8a-4c4f-8c4f-1b8a5c4c5c4f")]
public record DebitAccountOpened(string Name, double InitialBalance);

[EventType("41b5c5c4-5b8a-4c4f-8c4f-1b8a5c4c5c4f")]
public record DepositToDebitAccountPerformed(double Amount);

[EventType("51b5c5c4-5b8a-4c4f-8c4f-1b8a5c4c5c4f")]
public record WithdrawalFromDebitAccountPerformed(double Amount);

[EventType("61b5c5c4-5b8a-4c4f-8c4f-1b8a5c4c5c4f")]
public record ItemAddedToCart(string ItemId, string ItemName, double Price);
