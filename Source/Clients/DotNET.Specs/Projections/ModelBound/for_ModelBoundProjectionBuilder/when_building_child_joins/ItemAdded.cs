// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_child_joins;

[EventType]
public record ItemAdded(Guid ProductId, int Quantity);

[EventType]
public record ProductRegistered(string Name);

public record Order(
    [ChildrenFrom<ItemAdded>(key: nameof(ItemAdded.ProductId))] IEnumerable<OrderLine> Lines);

public record OrderLine(
    [Key] Guid ProductId,
    int Quantity,
    [Join<ProductRegistered>(eventPropertyName: nameof(ProductRegistered.Name))] string ProductName);

public record OrderWithPropertyChildren(
    [ChildrenFrom<ItemAdded>(key: nameof(ItemAdded.ProductId))] IEnumerable<PropertyOrderLine> Lines);

public class PropertyOrderLine
{
    [Key]
    public Guid ProductId { get; set; }

    [Join<ProductRegistered>(eventPropertyName: nameof(ProductRegistered.Name))]
    public string ProductName { get; set; }
}

public record OrderWithExplicitIdentity(
    [ChildrenFrom<ItemAdded>(key: nameof(ItemAdded.ProductId), identifiedBy: nameof(ExplicitOrderLine.ItemId))] IEnumerable<ExplicitOrderLine> Lines);

public record ExplicitOrderLine(
    Guid ItemId,
    [Join<ProductRegistered>(eventPropertyName: nameof(ProductRegistered.Name))] string ProductName);

public record OrderWithExplicitOn(
    [ChildrenFrom<ItemAdded>(key: nameof(ItemAdded.ProductId))] IEnumerable<ExplicitJoinOrderLine> Lines);

public record ExplicitJoinOrderLine(
    [Key] Guid ProductId,
    Guid OtherProductId,
    [Join<ProductRegistered>(on: nameof(OtherProductId), eventPropertyName: nameof(ProductRegistered.Name))] string ProductName);

public record ProductDetails(
    [property: Join<ProductRegistered>(eventPropertyName: nameof(ProductRegistered.Name))] string ProductName);

public record OrderWithNestedProduct(
    [Nested] ProductDetails? Product);

public record OrderWithNestedProductInChildren(
    [ChildrenFrom<ItemAdded>(key: nameof(ItemAdded.ProductId))] IEnumerable<OrderLineWithNestedProduct> Lines);

public record OrderLineWithNestedProduct(
    [Key] Guid ProductId,
    [Nested] ProductDetails? Product);

public record OrderWithoutChildIdentity(
    [ChildrenFrom<ItemAdded>] IEnumerable<UnidentifiedOrderLine> Lines);

public record UnidentifiedOrderLine(
    int Quantity,
    [Join<ProductRegistered>(eventPropertyName: nameof(ProductRegistered.Name))] string ProductName);
