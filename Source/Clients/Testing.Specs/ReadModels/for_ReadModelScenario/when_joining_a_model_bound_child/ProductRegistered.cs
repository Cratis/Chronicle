// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_joining_a_model_bound_child;

[EventType]
public record ProductRegistered(string Name);

[EventType]
public record ProductRenamed(string Name);

[EventType]
public record OrderPlaced(string CustomerName);

[EventType]
public record ItemAddedToOrder(Guid ProductId, int Quantity);

[Passive]
[FromEvent<OrderPlaced>]
public record Order(
    [Key] Guid Id,
    string CustomerName,
    [ChildrenFrom<ItemAddedToOrder>(key: nameof(ItemAddedToOrder.ProductId))] IEnumerable<OrderLine> Lines);

public record OrderLine(
    [Key] Guid ProductId,
    int Quantity,
    [Join<ProductRegistered>(eventPropertyName: nameof(ProductRegistered.Name))]
    [Join<ProductRenamed>(eventPropertyName: nameof(ProductRenamed.Name))]
    string ProductName);
