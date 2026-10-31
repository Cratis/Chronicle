// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class two_orders_with_the_same_product_line : an_order_projection_grain_with_lines_joined_to_products
{
    protected const string SecondOrder = "00000000-0000-0000-0000-000000000002";
    protected AppendedEvent[] _events;

    async Task Establish() => _events =
    [
        await ProductRegistered(Kettle, "Kettle"),
        await OrderPlaced("Ada"),
        await ItemAddedToOrder(Kettle, 1),
        await Append(SecondOrder, "OrderPlaced", new { CustomerName = "Grace" }),
        await Append(SecondOrder, "ItemAddedToOrder", new { ProductId = Guid.Parse(Kettle), Quantity = 2 }),
        await ProductRenamed(Kettle, "Electric kettle")
    ];
}
