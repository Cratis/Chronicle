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

[EventType]
public record ProductRegistered(string Name, string Description, double Price);

[EventType]
public record OrderPlaced(string CustomerName, int Quantity, double TotalAmount);

[EventType]
public record ProductRegisteredInInventory(string ProductName, DateTimeOffset RegisteredAt);

[EventType]
public record ItemsAddedToInventory(int Quantity, DateTimeOffset OccurredAt);

[EventType]
public record ItemsRemovedFromInventory(int Quantity, DateTimeOffset OccurredAt);

[EventType]
public record UserRegisteredWithCustomId(Guid UserId, string Email, string Name);

[EventType]
public record ReadModelRemoved(Guid Id);

[EventType]
public record ReadModelRemovedJoin(Guid Id);

[EventType]
public record ChildItemRemoved(Guid ParentId, Guid ItemId);

[EventType]
public record ChildItemRemovedJoin(Guid ItemId);

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
